﻿(* server protocol
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

module Internal =
   type GamesMessage =
   | Request of string * uint32 * Command * (Command -> unit) // gameid, playerid, command, send
   | Create of string[] * (Command -> unit) * AsyncReplyChannel<string> // map, send, reply

   let newGame gameId (map : Map) onGameOver =
      let delay_between_moves = 100 // ms
      let playerSends = System.Collections.Generic.List()
      let myGame = new HvZ.Common.Game(map)
      let nextId =
         let iter = (Seq.initInfinite uint32).GetEnumerator()
         fun () ->
            iter.MoveNext () |> ignore
            iter.Current
      MailboxProcessor.Start(fun inbox ->
         let rec loop (lastMoveTime : System.DateTime) numPlayers =
            async {
               let delay = int (System.DateTime.Now-lastMoveTime).TotalMilliseconds
               if delay < 0 then
                  playerSends.ForEach (fun x -> x Move)
                  return! loop System.DateTime.Now numPlayers
               else
                  let! input = inbox.TryReceive(delay)
                  match input with
                  | None ->
                     playerSends.ForEach (fun x -> x Move)
                     return! loop System.DateTime.Now numPlayers
                  | Some (playerId, cmd, send) ->
                     let checkOwnPlayer wId f =
                        if playerId <> wId then send (No "You can only control your own walker, not anyone else's.")
                        elif f () then playerSends.ForEach (fun x -> x cmd)
                        else send (No "I couldn't execute that command, sorry.")
                     match cmd with
                     | Forward (wId, dist) -> checkOwnPlayer wId (fun () -> myGame.Forward(wId, dist))
                     | Left (wId, degrees) -> checkOwnPlayer wId (fun () -> myGame.Left(wId, degrees))
                     | Right (wId, degrees) -> checkOwnPlayer wId (fun () -> myGame.Right(wId, degrees))
                     | Eat wId -> checkOwnPlayer wId (fun () -> myGame.Eat(wId))
                     | TakeFood (wId, fromWhere) -> checkOwnPlayer wId (fun () -> myGame.TakeFood(wId, fromWhere))
                     | TakeSocks (wId, fromWhere) -> checkOwnPlayer wId (fun () -> myGame.TakeSocks(wId, fromWhere))
                     | Throw (wId, heading) -> checkOwnPlayer wId (fun () -> myGame.Throw(wId, heading))
                     | HumanJoin (x, name) when x = gameId ->
                        if not <| map.AddHuman(nextId (), name) then failwith "I *told* you this would happen.  Now fix it."
                     | ZombieJoin (x, name) when x = gameId ->
                        if not <| map.AddZombie(nextId (), name) then failwith "Hey moron, fix what you need to fix."
                     | _ ->
                        send (No "This command is something that I tell clients.  Clients don't get to tell it to me.")
                     return! loop lastMoveTime numPlayers
            }
         and waitForPlayers () =
            async {
               let! input = inbox.TryReceive(1000)
               let doAdd addFunc playerId send createCommand =
                  if addFunc () then
                     playerSends.Add send
                     let x, y, heading =
                        let h = map.Walker playerId
                        h.Position.X, h.Position.Y, h.Heading
                     playerSends.ForEach (fun f ->
                        f (Human (playerId, x, y, heading))
                     )
                     loop System.DateTime.Now 1
                  else
                     send (No "There aren't any slots left for that kind of player on this map.")
                     waitForPlayers ()
               match input with
               | None -> onGameOver () // that's it -- no players in here for 1s -- game ended!
               | Some (playerId, HumanJoin(x,name), send) when x = gameId ->
                  return! doAdd (fun () -> map.AddHuman(playerId, name)) playerId send (fun x y heading -> Human (playerId, x, y, heading))
               | Some (playerId, ZombieJoin(x,name), send) when x = gameId ->
                  return! doAdd (fun () -> map.AddZombie(playerId, name)) playerId send (fun x y heading -> Zombie (playerId, x, y, heading))
               | Some (_, _, send) ->
                  send (No "There are no players in the game yet, so no commands can be issued to players yet.")
                  return! waitForPlayers ()
            }
         loop System.DateTime.Now 0
      )

   let gamesProcessor =
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
                     let map = HvZ.Common.Map(map)
                     gamesList.Add(id, newGame id map gameOver)
                     printfn "Sending game ID."
                     send (CreateOK id)
                     printfn "Game ID sent."
                     reply.Reply (id)
                  with
                  | e ->
                     send (No e.Message)
                     reply.Reply null
               | Request (gameId, playerId, cmd, send) ->
                  match gamesList.TryGetValue gameId with
                  | true, v -> v.Post (playerId, cmd, send)
                  | false, _ -> send (No "That game doesn't exist (maybe it's just ended?).")
                  return! loop ()
            }
         loop ()
      )

let handleRequest playerId status cmd send =
   printfn "Received %A from player %d" cmd playerId
   let send x =
      printfn "Sending %A to player %d" cmd playerId
      send x
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
