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

let handleRequest (s : string) = ()

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
            | n when n+offset < 3 ->
               printfn "%s felt like sending me an invalid packet.  I felt like killing that connection.  Terminated." clientId // and die.
               client.Close();
               stream.Close()
            | _ ->
               match Array.sub buffer 0 3 |> Encoding.ASCII.GetString |> System.Int32.TryParse with
               | false, _ ->
                  printfn "%s sending a corrupt packet.  Goodbye, client." clientId
                  client.Close()
                  stream.Close()
               | _, n when n < 0 ->
                  printfn "%s sending a corrupt packet.  Goodbye, client." clientId
                  client.Close()
                  stream.Close()
               | true, expectedLen ->
                  if n+offset >= expectedLen-3 then // ok, received it all.
                     let s = Encoding.ASCII.GetString (Array.sub buffer 3 expectedLen)
                     printfn "%s: '%s'" clientId s
                     handleRequest s
                     printfn "Offset %d, len %d, expected %d" offset n expectedLen
                     {0..buffer.Length-(expectedLen+4)} |> Seq.iter (fun i -> buffer.[i] <- buffer.[i+expectedLen+3]) // sloooooooooooooooooooooooooow
                     do! receive ((offset+n)-(expectedLen+3))
                  else // continue receiving.
                     do! receive (offset+n)
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
