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

open HvZ.Networking
open HvZ.Common
open System.Net
open System.Net.Sockets
open System.Text.RegularExpressions
open System.Text
open System

module Internal =
   type internal GamesMessage =
   | Request of string * uint32 * Command * (Command -> unit) // gameid, playerid, command, send
   | Create of string[] * (Command -> unit) * AsyncReplyChannel<string> // map, send, reply

   let internal newGame gameId (map : HvZ.Map) onGameOver =
      let delay_between_moves = 100 // ms
      let playerSends = System.Collections.Generic.List()
      let myGame = new HvZ.Common.Game(map)
      myGame.OnPlayerRemoved
      |> Event.add (fun args ->
         playerSends.RemoveAll(Predicate(fun (id,_) -> id = args.PlayerId)) |> ignore
      )
      let sendAll cmd =
         playerSends.RemoveAll (fun (_,x) ->
            try
               x cmd
               false
            with
            | e -> 
               printfn "Client connection errored, removing it: %A" e
               true
         ) |> ignore
      MailboxProcessor.Start(fun inbox ->
         let doAdd addFunc playerId send command =
            if addFunc () then
               playerSends.Add (playerId, send)
               let x, y, heading =
                  let w = map.Walker playerId
                  w.Position.X, w.Position.Y, w.Heading
               sendAll command
            else
               send (No "There aren't any slots left for that kind of player on this map.")
         let rec loop (lastMoveTime : System.DateTime) =
            async {
               if playerSends.Count = 0 then
                  return! waitForPlayers ()
               else
                  let delay = int (System.DateTime.Now-lastMoveTime).TotalMilliseconds
                  if delay < 0 then
                     sendAll Move // have to send before moving.  A player might die during the update, which would remove the player from playerSends.
                     myGame.Update ()
                     return! loop System.DateTime.Now
                  else
                     let! input = inbox.TryReceive(delay_between_moves)
                     match input with
                     | None ->
                        sendAll Move // have to send before moving.  A player might die during the update, which would remove the player from playerSends.
                        myGame.Update ()
                        return! loop System.DateTime.Now
                     | Some (playerId, cmd, send) ->
                        let checkOwnPlayer wId f =
                           if playerId <> wId then send (No "You can only control your own walker, not anyone else's.")
                           else
                              match f () with // yes, I know I'm killing the traditional C# semantics.
                              | null -> sendAll cmd
                              | error -> send (No (sprintf "You asked me to %O, but I couldn't because %s" cmd error))
                        match cmd with
                        | Forward (wId, dist) -> checkOwnPlayer wId (fun () -> myGame.Forward(wId, dist))
                        | Turn (wId, degrees) -> checkOwnPlayer wId (fun () -> myGame.Turn(wId, degrees))
                        | Eat wId -> checkOwnPlayer wId (fun () -> myGame.Eat(wId))
                        | TakeFood (wId, fromWhere) -> checkOwnPlayer wId (fun () -> myGame.TakeFood(wId, fromWhere))
                        | TakeSocks (wId, fromWhere) -> checkOwnPlayer wId (fun () -> myGame.TakeSocks(wId, fromWhere))
                        | Throw (wId, heading) -> checkOwnPlayer wId (fun () -> myGame.Throw(wId, heading))
                        | HumanJoin (x, name) when x = gameId ->
                           if not <| map.AddHuman(playerId, name) then send (No "This game is full, sorry.")
                           else doAdd (fun () -> map.AddHuman(playerId, name)) playerId send (Human (playerId, name))
                        | ZombieJoin (x, name) when x = gameId ->
                           if not <| map.AddZombie(playerId, name) then send (No "This game is full, sorry.")
                           else doAdd (fun () -> map.AddZombie(playerId, name)) playerId send (Zombie (playerId, name))
                        | _ ->
                           send (No "This command is something that I tell clients.  Clients don't get to tell it to me.")
                        return! loop lastMoveTime
            }
         and waitForPlayers () =
            async {
               let! input = inbox.TryReceive(5000)
               match input with
               | None ->
                  printfn "There are no players left in %s; the game is declared to be over!" gameId
                  onGameOver () // that's it -- no players in here for 5s -- game ended!
               | Some (playerId, HumanJoin(x,name), send) when x = gameId ->
                  doAdd (fun () -> map.AddHuman(playerId, name)) playerId send (Human (playerId, name))
                  return! loop System.DateTime.Now
               | Some (playerId, ZombieJoin(x,name), send) when x = gameId ->
                  doAdd (fun () -> map.AddZombie(playerId, name)) playerId send (Zombie (playerId, name))
                  return! loop System.DateTime.Now
               | Some (_, _, send) ->
                  send (No "There are no players in the game yet, so no commands can be issued to players yet.")
                  return! waitForPlayers ()
            }
         waitForPlayers ()
      )

   let internal gamesProcessor =
      let gamesList = System.Collections.Generic.Dictionary<string, MailboxProcessor<_>>()
      let nextGamesId =
         let s = Seq.initInfinite (fun _ -> Array.sub (System.Guid.NewGuid().ToByteArray()) 0 15 |> System.Convert.ToBase64String)
         let iter = s.GetEnumerator()
         fun () ->
            ignore <| iter.MoveNext()
            iter.Current
      MailboxProcessor.Start(fun inbox ->
         let rec loop () =
            async {
               let! input = inbox.Receive()
               match input with
               | Create (map, send, reply) ->
                  let id = nextGamesId ()
                  printfn "Creating a new game, id=%s" id
                  let gameOver () = gamesList.Remove id |> ignore // maybe also send out a notification that the game doesn't exist any more??
                  try
                     let map = HvZ.Map(map)
                     let processor = newGame id map gameOver
                     processor.Error
                     |> Event.add (printfn "ERROR during game %s: %A" id)
                     gamesList.Add(id, processor)
                     send (CreateOK id)
                     reply.Reply (id)
                  with
                  | e ->
                     send (No e.Message)
                     reply.Reply null
                  return! loop ()
               | Request (gameId, playerId, cmd, send) ->
                  match gamesList.TryGetValue gameId with
                  | true, v -> v.Post (playerId, cmd, send)
                  | false, _ -> send (No "That game doesn't exist (maybe it's just ended?).")
                  return! loop ()
            }
         loop ()
      )

let internal handleRequest playerId status cmd send =
(*
   printfn "Received %A from player %d" cmd playerId
   let send x =
      printfn "Sending %A to player %d" x playerId
      send x
*)
   match status, cmd with
   | OutOfGame, HumanJoin (gameId, _) | OutOfGame, ZombieJoin (gameId, _) ->
      Internal.gamesProcessor.Post (Internal.GamesMessage.Request(gameId, playerId, cmd, send))
      InGame gameId // WARNING: this is just wrong.  It *will* cause trouble.  Fix it up later.
   | OutOfGame, Create map ->
      let gameId = Internal.gamesProcessor.PostAndReply(fun reply -> Internal.GamesMessage.Create(map.Split [|'\n';'\r'|], send, reply))
      if gameId = null then OutOfGame else InGame gameId
   | OutOfGame, _ ->
      send (No "You need to either create or join a game first.")
      OutOfGame
   | InGame gameId, _ ->
      Internal.gamesProcessor.Post (Internal.GamesMessage.Request(gameId, playerId, cmd, send))
      InGame gameId

[<EntryPoint>]
let main argv = 
   Console.Title <- "HvZ Server"
   let listener = TcpListener(IPAddress.Any, 2310)
   try
      listener.Server.LingerState <- LingerOption(false, 1)
      listener.Start()
      async {
         printfn "Server up, listening on 2310"
         while true do
            let! client = Async.FromBeginEnd(listener.BeginAcceptTcpClient, listener.EndAcceptTcpClient)
            HvZ.Networking.Internal.serverHandleTcp client handleRequest |> ignore
      } |> Async.RunSynchronously
   finally
      listener.Stop()
   0
