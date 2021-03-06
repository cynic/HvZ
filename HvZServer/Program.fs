﻿open HvZ.Networking
open HvZ.Common
open System.Net
open System.Net.Sockets
open System.Text.RegularExpressions
open System.Text
open System

module Internal =
   type internal GamesMessage = string * Command * (Command -> unit) // connid, command, send

   let internal newGame gameId (map : HvZ.Map) onGameOver =
      let delay_between_moves = 100 // ms
      //let playerSends = System.Collections.Generic.List()
      let myGame = new HvZ.Common.Game(map)
      let connToPlayers = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<_> * _>()
      myGame.OnPlayerRemoved
      |> Event.add (fun args ->
         connToPlayers.Values
         |> Seq.iter (fun (v,_) -> v.Remove args.PlayerId |> ignore)
         // if there are any 'empty' connections, remove them now.
         connToPlayers.Keys
         |> Seq.toArray
         |> Seq.iter (fun k -> if (fst connToPlayers.[k]).Count = 0 then ignore <| connToPlayers.Remove k)
      )
      let timeDelay = ref 5000
      let sendAll cmd =
         connToPlayers
         |> Seq.choose (fun kvp ->
            let _,x = kvp.Value
            if x cmd then None
            else Some kvp.Key
         )
         |> Seq.toArray
         |> Seq.iter (fun k -> connToPlayers.Remove k |> ignore)
      MailboxProcessor.Start(fun inbox ->
         let doAdd addFunc connId playerId guid send =
            if addFunc () then
               timeDelay := 1000
               //playerSends.Add (playerId, send)
               match connToPlayers.TryGetValue connId with
               | true, (v,_) -> v.Add playerId
               | _ -> connToPlayers.[connId] <- (new System.Collections.Generic.List<_>([playerId]), send)
               sendAll (JoinOK (playerId, guid, map.GetSerializedData()))
            else
               if not <| send (GameNo "There aren't any slots left for players on this map.") then
                  connToPlayers.Remove connId |> ignore
         let gameEnded = ref false
         myGame.OnGameEnded
         |> Event.add (fun _ -> gameEnded := true)
         let rec loop (lastMoveTime : System.DateTime) =
            async {
               //if playerSends.Count = 0 then
               if connToPlayers.Count = 0 then
                  return! waitForPlayers ()
               elif !gameEnded then
                  printfn "Game %s has ended." gameId
                  onGameOver ()
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
                     | Some (connId, cmd, send : Command -> bool) ->
                        let checkOwnPlayer wId f =
                           match connToPlayers.TryGetValue connId with
                           | true, (l,_) ->
                              if not (l.Contains wId) then
                                 //send (No "You can only control your own walker, not anyone else's.")
                                 printfn "Received a command for walker %d from connection %s, but that connection is not associated with that walker." wId connId
                                 () // just ignore the command.
                              else
                                 match f () with // yes, I know I'm killing the traditional C# semantics.
                                 | null -> sendAll cmd
                                 | error ->
                                    if not <| send (No (wId, sprintf "You asked me to %O, but I couldn't because %s" cmd error)) then
                                       connToPlayers.Remove connId |> ignore
                           | _ -> () // this connection has already been dropped.
                        match cmd with
                        | Forward (wId, dist) -> checkOwnPlayer wId (fun () -> myGame.Forward(wId, dist))
                        | Turn (wId, degrees) -> checkOwnPlayer wId (fun () -> myGame.Turn(wId, degrees))
                        | Bite (wId, target) -> checkOwnPlayer wId (fun () -> myGame.Bite(wId, target))
                        | Eat wId -> checkOwnPlayer wId (fun () -> myGame.Eat(wId))
                        | TakeFood (wId, fromWhere) -> checkOwnPlayer wId (fun () -> myGame.TakeFood(wId, fromWhere))
                        | TakeSocks (wId, fromWhere) -> checkOwnPlayer wId (fun () -> myGame.TakeSocks(wId, fromWhere))
                        | Throw (missileId, wId, heading) -> checkOwnPlayer wId (fun () -> myGame.Throw(missileId, wId, heading))
                        | HumanJoin (x, guid, name) when x = gameId ->
                           let playerId = nextUIntId ()
                           //eprintfn "human playerId for %s = %d" guid playerId
                           doAdd (fun () -> map.AddHuman(playerId, name)) connId playerId guid send
                        | ZombieJoin (x, guid, name) when x = gameId ->
                           let playerId = nextUIntId ()
                           //eprintfn "zombie playerId for %s = %d" guid playerId
                           doAdd (fun () -> map.AddZombie(playerId, name)) connId playerId guid send
                        | _ ->
                           if not <| send (GameNo (sprintf "%O is something that I tell clients.  Clients don't get to tell it to me." cmd)) then
                              connToPlayers.Remove connId |> ignore
                        return! loop lastMoveTime
            }
         and waitForPlayers () =
            async {
               let! input = inbox.TryReceive(!timeDelay)
               match input with
               | None ->
                  printfn "There are no players left in %s; the game is declared to be over!" gameId
                  onGameOver () // that's it -- no players in here for 5s -- game ended!
               | Some (connId, HumanJoin(x, guid, name), send) when x = gameId ->
                  let playerId = nextUIntId ()
                  //eprintfn "human playerId for %s = %d" guid playerId
                  doAdd (fun () -> map.AddHuman(playerId, name)) connId playerId guid send
                  return! loop System.DateTime.Now
               | Some (connId, ZombieJoin(x, guid, name), send) when x = gameId ->
                  let playerId = nextUIntId ()
                  //eprintfn "zombie playerId for %s = %d" guid playerId
                  doAdd (fun () -> map.AddZombie(playerId, name)) connId playerId guid send
                  return! loop System.DateTime.Now
               | Some (connId, _, send) ->
                  if not <| send (GameNo "There are no players in the game yet, so no commands can be issued to players yet.") then
                     connToPlayers.Remove connId |> ignore
                  return! waitForPlayers ()
            }
         waitForPlayers ()
      )

   let internal gamesProcessor =
      let gamesList = System.Collections.Generic.Dictionary<string, MailboxProcessor<_>>()
      let connToGame = System.Collections.Generic.Dictionary<string, string>()
      MailboxProcessor.Start(fun inbox ->
         let rec loop () =
            async {
               let! input = inbox.Receive()
               match input with
               | connId, Create(gameName, mapData), (send : Command -> bool) ->
                  if gamesList.ContainsKey gameName then
                     send (GameNo "There's already a game with this name.  Choose a different name.") |> ignore
                  else
                     printfn "Creating a new game, name=%s" gameName
                     let gameOver () =
                        // maybe also send out a notification that the game doesn't exist any more??
                        gamesList.Remove gameName |> ignore
                        if connToGame.[connId] = gameName then
                           connToGame.Remove connId |> ignore                        
                     try
                        let map = HvZ.Map()
                        map.PopulateFromSerializedData(mapData)
                        let processor = newGame gameName map gameOver
                        processor.Error
                        |> Event.add (fun exc ->
                           printfn "ERROR during game '%s'; aborting:\n%A" gameName exc                           
                           gameOver ()
                        )
                        gamesList.Add(gameName, processor)
                        connToGame.Add(connId, gameName)
                     with
                     | e -> send (GameNo e.Message) |> ignore
                  return! loop ()
               | connId, ((HumanJoin (gameId, _, _)) as cmd), send | connId, ((ZombieJoin (gameId, _, _)) as cmd), send ->
                  match gamesList.TryGetValue gameId with
                  | true, v ->
                     connToGame.[connId] <- gameId
                     v.Post (connId, cmd, send)
                  | _ -> send (GameNo "That game doesn't exist (maybe it's just ended?)") |> ignore
                  return! loop ()
               | connId, cmd, send ->
                  match connToGame.TryGetValue connId with
                  | true, gameName ->
                     match gamesList.TryGetValue gameName with
                     | true, v -> v.Post (connId, cmd, send)
                     | _ -> send (GameNo "That game doesn't exist (maybe it's just ended?).") |> ignore
                  | _ -> send (GameNo "You need to create or join a game before sending any commands.") |> ignore
                  return! loop ()
            }
         loop ()
      )

let internal handleRequest connId cmd send =
      match cmd with
      | Forward _ | Turn _ -> () // eprintfn "Received request: %O" cmd
      | _ -> () // eprintfn "Received request: %O" cmd
      Internal.gamesProcessor.Post (connId, cmd, send)

[<EntryPoint>]
let main argv = 
   Console.Title <- "HvZ Server"
   let port = 2310
   let listener = TcpListener(IPAddress.Any, port)
   try
      listener.Server.LingerState <- LingerOption(false, 1)
      listener.Start()
      let myAddress =
         let host = Dns.GetHostName() |> Dns.GetHostEntry
         let rec getExternalIP (addrList : IPAddress list) ipv6 =
            match addrList with
            | [] ->
               match ipv6 with
               | Some addr -> addr
               | None -> "127.0.0.1"
            | x::xs ->
               if x.AddressFamily <> AddressFamily.InterNetwork && x.AddressFamily <> AddressFamily.InterNetworkV6 then
                  getExternalIP xs ipv6
               elif string x = "127.0.0.1" || string x = "::1" || (string x).StartsWith "fe80::" then
                  getExternalIP xs ipv6
               elif x.AddressFamily = AddressFamily.InterNetworkV6 then
                  getExternalIP xs (Some <| string x) // if we hit an ipv6, well, take note of it, but prefer ipv4
               else
                  string x // p
         getExternalIP (List.ofSeq host.AddressList) None
      async {
         printfn "Server up, listening on port %d" port
         printfn "To connect to this server from your code, use something like"
         printfn "   Game g = new Game(\"%s\", %d);" myAddress port
         printfn "instead of"
         printfn "   Game g = new Game();"
         printfn "Press Ctrl-C or close this window to stop the server."
         while true do
            let! client = Async.FromBeginEnd(listener.BeginAcceptTcpClient, listener.EndAcceptTcpClient)
            HvZ.Networking.Internal.serverHandleTcp client handleRequest |> ignore
      } |> Async.RunSynchronously
   finally
      listener.Stop()
   0
