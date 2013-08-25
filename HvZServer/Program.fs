// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

(* server protocol
C->S:
   Move forward (who)
   Throw sock (who, heading)
   Turn (who, degrees - float)
   Eat (who, whom)
   Take (who, what, fromwhere)

S->C:
   Hit (who, whom)
   Dead (who, whom)
   Starved (who)
   No
   Refresh
   Everything in C->S
   LeftGame (who)
   Join (who) <--------- muuuuuuuuch later
   Stats (who)
*)

(*
Planned:

- Length-prefixed messages: space-padded 3-byte ASCII length-in-bytes, then rest of data (can be unicode?)
- Gametime in each message
- Refresh first?  We'll see.
- Plain TCP async
- Leave-message on TCP Reset <- this will be frequent!

*)

open System.Net
open System.Net.Sockets
open System.Text.RegularExpressions
open System.Text

module Regex =
   let prefix = """(?<game>.{1,8})-(?<who>\d+) """
   let forward = prefix + """forward"""
   let turnRight = prefix + """right (?<degrees>\d{1,3}\.\d{2})"""
   let turnLeft = prefix + """left (?<degrees>\d{1,3}\.\d{2})"""
   let eat = prefix + """eat (?<target>\d+)"""
   let take = prefix + """take (?<itemtype>\d{1,2}) (?<target>\d+)"""

let handleRequest (s : string) = printfn "Command: '%s'" s

type ParseCommandResult =
| Corrupt
| OK of int // int = offset for new command reception

let handle (client : TcpClient) =
   let stream = client.GetStream()
   let buffer = Array.zeroCreate 1020
   let clientId = string client.Client.RemoteEndPoint
   let rec receive offset =
      async {
         let! n = stream.AsyncRead(buffer, offset, buffer.Length-offset)
         printfn "Received %d bytes" n
         try
            match n with
            | 0 -> // shutdown.
               printfn "Connection closed (%s)" clientId // and die.
               client.Close()
               stream.Close()
            | n when n+offset < 3 -> do! receive (n+offset) // trickling? Ah, well.  Keep receiving.
            | _ ->
               let rec parseCommands offset remaining =
                  if remaining >= 3 then
                     match Array.sub buffer offset 3 |> Encoding.ASCII.GetString |> System.Int32.TryParse with
                     | false, _ -> Corrupt
                     | _, n when n < 0 -> Corrupt
                     | true, expectedLen ->
                        if remaining-3 >= expectedLen then // ok, received it all.
                           let s = Encoding.ASCII.GetString (Array.sub buffer (offset+3) expectedLen)
                           handleRequest s // should really be a POST to a game-agent...
                           //printfn "parse-local Offset %d, remaining %d, expected %d" offset remaining expectedLen
                           parseCommands (offset+expectedLen+3) (remaining-(expectedLen+3))
                        else // continue receiving.
                           OK remaining
                  else OK remaining
               match parseCommands 0 (offset+n) with
               | OK 0 -> do! receive 0 // I expect this path to be taken in most cases.
               | OK remaining ->
                  let used = (offset+n)-remaining
                  //printfn "used: %d, remaining: %d" used remaining
                  for i = 0 to (buffer.Length-used)-1 do
                     buffer.[i] <- buffer.[i+used] // sloooooooooooooooooooooooooooooow
                  do! receive remaining
               | Corrupt ->
                  printfn "Client %s sent corrupt packet.  Goodbye, client." clientId
                  stream.Close()
                  client.Close()
         with
         | e -> // Client's sending me crap.  To save myself from resync-to-stream annoyances, I'm going to kill the client connection.
            printfn "Error'd (%s): %A" clientId e
            stream.Close()
      }
   receive 0 |> Async.Start

[<EntryPoint>]
let main argv = 
   let listener = TcpListener(IPAddress.Any, 2310)
   try
      listener.Server.LingerState <- LingerOption(false, 1)
      listener.Start()
      async {
         printfn "Server up, listening on 2310"
         while true do
            let! client = Async.FromBeginEnd(listener.BeginAcceptTcpClient, listener.EndAcceptTcpClient)
            handle client
      } |> Async.RunSynchronously
   finally
      listener.Stop()
   0
