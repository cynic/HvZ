namespace HvZNetworking

open System.Net
open System.Net.Sockets

(*
How to add new commands:

1. Add a line for the command (including the parameters for it), to the Command type.
2. Add a line for the command to the 'toProtocol' function.
3. Add a line for the command to the 'fromString' function.
4. (only necessary if the Client needs to send this command) Add a member to the HvZConnection class.

aaand, that's it.  You've now successfully got a Brand Spanking New valid protocol command.  Congratulations.
*)

type IIdentified =
   interface
      abstract member Id : uint32 with get
   end

type Command =
| Forward of uint32 * float // walkerId * distance
| Left of uint32 * float // walkerId * degrees
| Right of uint32 * float // walkerId * degrees
| Eat of uint32 // walkerId
| TakeFood of uint32 * uint32 // who-takes-it * taken-from-where
| TakeSocks of uint32 * uint32 // who-takes-it * taken-from-where
| Throw of uint32 * float // walkerId * heading
| Join of string // gameId
| JoinOK
| Hello of uint32 // playerId
| Create of uint32 * uint32 // width * height
| ListStart
| Game of string // gameId /// also functions as CreateOK
| ListEnd
| Human of uint32 * uint32 * uint32 * float // walkerId * x * y * heading
| Zombie of uint32 * uint32 * uint32 * float // walkerId * x * y * heading
| No of string // reason for rejection

[<AutoOpen>]
module Internal =
   open System.Text
   open System.Text.RegularExpressions

   let toProtocol cmd =
      let s =
         match cmd with
         | Forward (wId, dist) -> sprintf "forward %d %.2f" wId dist
         | Left (wId, degrees) -> sprintf "left %d %.2f" wId degrees
         | Right (wId, degrees) -> sprintf "right %d %.2f" wId degrees
         | Eat wId -> sprintf "eat %d" wId
         | TakeFood (wId, fromWhere) -> sprintf "takefood %d %d" wId fromWhere
         | TakeSocks (wId, fromWhere) -> sprintf "takesocks %d %d" wId fromWhere
         | Throw (wId, heading) -> sprintf "throw %d %.2f" wId heading
         | Join gameId -> sprintf "join %s" gameId
         | JoinOK -> sprintf "joinok"
         | Hello playerId -> sprintf "hello %d" playerId
         | Create (width, height) -> sprintf "create %d %d" width height
         | Game gameId -> sprintf "game %s" gameId
         | ListStart -> sprintf "begin"
         | ListEnd -> sprintf "end"
         | Human (walkerId, x, y, heading) -> sprintf "human %d %d %d %.2f" walkerId x y heading
         | Zombie (walkerId, x, y, heading) -> sprintf "zombie %d %d %d %.2f" walkerId x y heading
         | No why -> sprintf "no %s" why // reason for rejection
      Encoding.UTF8.GetBytes (sprintf "%3d%s" (Encoding.UTF8.GetByteCount s) s)

   let fromString =
      let makeMatcher reString (f : _[] -> Command) = 
         let re = Regex(reString, RegexOptions.Compiled ||| RegexOptions.IgnoreCase ||| RegexOptions.Singleline)
         re, f
      let matchers = [|
         makeMatcher @"^forward (\d+) (\d+\.\d{2})$" (fun m -> Forward(uint32 m.[1], float m.[2]))
         makeMatcher @"^left (\d+) (\d+\.\d{2})$" (fun m -> Left(uint32 m.[1], float m.[2]))
         makeMatcher @"^right (\d+) (\d+\.\d{2})$" (fun m -> Right(uint32 m.[1], float m.[2]))
         makeMatcher @"^eat (\d+)$" (fun m -> Eat(uint32 m.[1]))
         makeMatcher @"^takefood (\d+) (\d+)$" (fun m -> TakeFood(uint32 m.[1], uint32 m.[2]))
         makeMatcher @"^takesocks (\d+) (\d+)$" (fun m -> TakeSocks(uint32 m.[1], uint32 m.[2]))
         makeMatcher @"^throw (\d+) (\d+\.\d{2}$" (fun m -> Throw(uint32 m.[1], float m.[2]))
         makeMatcher @"^join (.{1,8})$" (fun m -> Join(m.[1]))
         makeMatcher @"^joinok$" (fun _ -> JoinOK)
         makeMatcher @"^hello (\d+)$" (fun m -> Hello(uint32 m.[1]))
         makeMatcher @"^create (\d+) (\d+)$" (fun m -> Create(uint32 m.[1], uint32 m.[2]))
         makeMatcher @"^game (.{1,8})$" (fun m -> Game(m.[1]))
         makeMatcher @"^begin$" (fun _ -> ListStart)
         makeMatcher @"^end$" (fun _ -> ListEnd)
         makeMatcher @"^human (\d+) (\d+) (\d+) (\d+\.\d{2}$" (fun m -> Human(uint32 m.[1], uint32 m.[2], uint32 m.[3], float m.[4]))
         makeMatcher @"^zombie (\d+) (\d+) (\d+) (\d+\.\d{2}$" (fun m -> Zombie(uint32 m.[1], uint32 m.[2], uint32 m.[3], float m.[4]))
         makeMatcher @"^no (.+)$" (fun m -> No(m.[1]))
      |]
      fun txt ->
         matchers |> Array.tryPick (fun (re, f) ->
            let m = re.Match txt
            if m.Success then
               try
                  let args = m.Groups |> Seq.cast<Group> |> Seq.map (fun x -> x.Value) |> Seq.toArray
                  Some (f args)
               with
               | _ -> None // hmm, nope.
            else None
         )

   let writeTo (s : System.IO.Stream) c =
      let arr = toProtocol c
      s.Write (arr, 0, arr.Length)

   let nextPlayerId =
      let v = ref 0
      fun () ->
         System.Threading.Interlocked.Increment v |> uint32

   type ParseCommandResult =
   | Corrupt
   | OK of int // int = offset for new command reception
   
   type ClientStatus =
   | InGame of string // gameId
   | OutOfGame

   type ShutdownReason =
   | CorruptStream
   | StreamClosed

   let safeGetString buf =
      try
         Some (Encoding.UTF8.GetString buf)
      with
      | _ -> None

   let safeGetInt str =
      safeGetString str
      |> Option.bind (fun s ->
         System.Int32.TryParse s
         |> fun (ok, i) -> if ok then Some i else None
      )

   let connection (client : TcpClient) onClosed onCommandReceived onAbnormalCommand =
      let buffer = Array.zeroCreate 1020
      let stream = client.GetStream ()
      MailboxProcessor.Start(fun inbox ->
         let rec receive offset =
            async {
               let! n = stream.AsyncRead (buffer, offset, buffer.Length-offset)
               //printfn "Received %d bytes" n
               match n with
               | 0 -> onClosed StreamClosed // shutdown.
               | n when n+offset < 3 -> do! receive (n+offset) // trickling? Ah, well.  Keep receiving.
               | _ ->
                  let rec parseCommands offset remaining =
                     if remaining >= 3 then
                        match safeGetInt (Array.sub buffer offset 3) with
                        | None -> Corrupt
                        | Some n when n < 0 -> Corrupt
                        | Some expectedLen ->
                           if remaining-3 >= expectedLen then // ok, received it all.
                              let s = safeGetString (Array.sub buffer (offset+3) expectedLen)
                              match s with
                              | None -> Corrupt
                              | Some s ->
                                 try
                                    match fromString s with
                                    | Some cmd -> onCommandReceived cmd stream
                                    | None -> onAbnormalCommand s stream
                                 with
                                 | e -> printfn "Got an error in client code: %A" e
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
                     stream.Close()
                     client.Close()
                     onClosed CorruptStream
            }
         receive 0
      )

   let serverHandleTcp (client : TcpClient) handleRequest =
      let stream = client.GetStream()
      let clientId = string client.Client.RemoteEndPoint
      let playerId = nextPlayerId ()
      Hello playerId |> writeTo stream
      let status = ref OutOfGame
      let onClosed = function
         | CorruptStream -> printfn "Corrupt stream from %s quitting." clientId
         | StreamClosed -> printfn "Connection to %s closed" clientId
      let onCommandReceived cmd stream =
         status := handleRequest playerId !status cmd (fun x -> writeTo stream x)
      let onAbnormalCommand s stream =
         No (sprintf "I couldn't understand the command you sent (%s)" s) |> writeTo stream
      connection client onClosed onCommandReceived onAbnormalCommand

   let clientHandleTcp (client : TcpClient) onClosed onCommandReceived =
      let stream = client.GetStream()
      let playerId = nextPlayerId ()
      let playerId = ref None
      let onClosed = function
         | CorruptStream -> failwith "There was an error on the server - ask your lecturer to look into it.  Sorry."
         | StreamClosed -> onClosed () // Goodbye, quite naturally.
      let onAbnormalCommand s _ =
         failwithf "The server sent me a weird command (%s).  Tell your lecturer." s
      connection client onClosed onCommandReceived onAbnormalCommand
      |> ignore

type CommandEventArgs(player : uint32, command : Command) =
   inherit System.EventArgs()
   member __.Player with get () = player
   member __.Command with get () = command

type HvZConnection() as this =
   let mutable status = OutOfGame
   let mutable playerId = None
   let dataEvent = new Event<System.EventHandler<CommandEventArgs>,CommandEventArgs>()
   let closedEvent = new Event<_>()
   let mutable conn : TcpClient option = None
   let onClosed () = closedEvent.Trigger ()
   let onCommandReceived cmd stream =
      match status, playerId, cmd with
      | _, None, Hello pId -> playerId <- Some pId
      | _, Some _, Hello _ -> printfn "Why is the server sending me multiple hello-messages??"
      | _, None, _ -> printfn "Server shouldn't be sending me anything... very weird!"
      | _, Some pIayerId, cmd ->
         dataEvent.Trigger (this, new CommandEventArgs(Option.get playerId, cmd))
   let send () =
      match conn with
      | Some conn ->
         let stream = conn.GetStream()
         writeTo stream
      | None -> failwith "You can't send messages to a server until you're connected to it."
   member __.ConnectToServer server =
      let client = new TcpClient(server, 2310, LingerState = LingerOption(false, 0))
      conn <- Some client
      clientHandleTcp client onClosed onCommandReceived
   member __.Send msg = send () msg
   member __.IsInGame with get () = match status with OutOfGame -> false | InGame _ -> true
   [<CLIEvent>]
   member __.OnCommandReceived = dataEvent.Publish
   [<CLIEvent>]
   member __.OnConnectionClosed = closedEvent.Publish
   interface System.IDisposable with
      member __.Dispose () =
         match conn with
         | Some conn -> (conn :> System.IDisposable).Dispose ()
         | None -> ()

   (* Here be members which hide the nasty details of Command interop *)

   member __.Forward distance = send () (Forward (Option.get playerId, distance))
   member __.Left degrees = send () (Left (Option.get playerId, degrees))
   member __.Right degrees = send () (Right (Option.get playerId, degrees))
   member __.Eat () = send () (Eat (Option.get playerId))
   member __.TakeFoodFrom (r : IIdentified) = send () (TakeFood (Option.get playerId, r.Id))
   member __.TakeSocksFrom (r : IIdentified) = send () (TakeSocks (Option.get playerId, r.Id))
   member __.Throw heading = send () (Throw (Option.get playerId, heading))   