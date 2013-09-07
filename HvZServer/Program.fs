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

open HvZNetworking
open System.Net
open System.Net.Sockets
open System.Text.RegularExpressions
open System.Text

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
