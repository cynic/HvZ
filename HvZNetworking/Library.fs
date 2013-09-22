namespace HvZ.Common

module Mxyzptlk =
   [<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("HvZCommon")>]
   do ()

type IIdentified =
   abstract member Id : uint32 with get

type Position(x : float, y : float) =
   let evt = Event<_,_>()
   let mutable x = x
   let mutable y = y

   member __.X
      with get () = x
      and internal set v =
         evt.Trigger(__, System.ComponentModel.PropertyChangedEventArgs "X")
         x <- v
   member __.Y
      with get () = y
      and internal set v =
         evt.Trigger (__, System.ComponentModel.PropertyChangedEventArgs "Y")
         y <- v

   interface System.ComponentModel.INotifyPropertyChanged with
      [<CLIEvent>]
      member __.PropertyChanged = evt.Publish

type ITakeSpace =
   abstract member Position : Position with get
   abstract member Radius : float with get

type IWalker =
   inherit ITakeSpace
   /// <summary>heading is in degrees, 0 is directly upwards</summary>
   abstract member Heading : float with get
   abstract member Name : string with get

   abstract member Health : float with get
(*
How to add new commands:

1. Add a line for the command (including the parameters for it), to the Command type.
2. Add a line for the command to the 'toProtocol' function.
3. Add a line for the command to the 'fromString' function.
4. (only necessary if the Client needs to send this command) Add a member to the HvZConnection class.

aaand, that's it.  You've now successfully got a Brand Spanking New valid protocol command.  Congratulations.
*)

type Command =
| Forward of uint32 * float // walkerId * distance
| Left of uint32 * float // walkerId * degrees
| Right of uint32 * float // walkerId * degrees
| Eat of uint32 // walkerId
| Bite of uint32 * uint32 // biter * bitten
| TakeFood of uint32 * uint32 // who-takes-it * taken-from-where
| TakeSocks of uint32 * uint32 // who-takes-it * taken-from-where
| Throw of uint32 * float // walkerId * heading
| ZombieJoin of string * string // gameId * name
| HumanJoin of string * string // gameId * name
| Hello of uint32 // playerId
| Create of string // mapdata
| ListStart
| CreateOK of string // gameId
| Game of string // gameId
| ListEnd
| Human of uint32 * float * float * float * string // walkerId * x * y * heading * name /// also functions as JoinOK
| Zombie of uint32 * float * float * float * string // walkerId * x * y * heading * name /// also functions as JoinOK
| Move
| No of string // reason for rejection

type ICommandInterpreter =
   abstract member Forward : walkerId:uint32 -> distance:float -> unit
   abstract member Left : walkerId:uint32 -> degrees:float -> unit
   abstract member Right : walkerId:uint32 -> degrees:float -> unit
   abstract member Eat : walkerId:uint32 -> unit
   abstract member Bite : walkerId:uint32 -> target:uint32 -> unit
   abstract member TakeFood : walkerId:uint32 -> resupplyId:uint32 -> unit // who-takes-it -> taken-from-where
   abstract member TakeSocks : walkerId:uint32 -> resupplyId:uint32 -> unit // who-takes-it -> taken-from-where
   abstract member Throw : walkerId:uint32 -> heading:float -> unit // walkerId -> heading
   abstract member Hello : walkerId:uint32 -> unit // playerId
   abstract member Create : mapdata:string -> unit // mapdata
   abstract member ListStart : unit -> unit
   abstract member CreateOK : gameId:string -> unit // gameId
   abstract member Game : gameId:string -> unit // gameId
   abstract member ListEnd : unit -> unit
   abstract member Human : walkerId:uint32 -> x:float -> y:float -> heading:float -> name:string -> unit // walkerId -> x -> y -> heading /// also functions as JoinOK
   abstract member Zombie : walkerId:uint32 -> x:float -> y:float -> heading:float -> name:string -> unit // walkerId -> x -> y -> heading /// also functions as JoinOK
   abstract member Move : unit -> unit
   abstract member No : reason:string -> unit // reason for rejection

[<Extension>]
type Command with
   static member Dispatch cmd (x : ICommandInterpreter) =
      match cmd with
      | Forward (wId, dist) -> x.Forward wId dist
      | Left (wId, degrees) -> x.Left wId degrees
      | Right (wId, degrees) -> x.Right wId degrees
      | Eat wId -> x.Eat wId
      | TakeFood (wId, fromWhere) -> x.TakeFood wId fromWhere
      | TakeSocks (wId, fromWhere) -> x.TakeSocks wId fromWhere
      | Throw (wId, heading) -> x.Throw wId heading
      | Hello playerId -> x.Hello playerId
      | Create mapdata -> x.Create mapdata
      | CreateOK gameId -> x.CreateOK gameId
      | Game gameId -> x.Game gameId
      | Bite (wId, target) -> x.Bite wId target
      | ListStart -> x.ListStart ()
      | ListEnd -> x.ListEnd ()
      | Human (walkerId, _x, y, heading, name) -> x.Human walkerId _x y heading name
      | Zombie (walkerId, _x, y, heading, name) -> x.Zombie walkerId _x y heading name
      | Move -> x.Move ()
      | No why -> x.No why
      | _ -> printfn "I shouldn't be receiving %A commands..." cmd // ignore?

type ClientStatus =
| InGame of string // gameId
| OutOfGame

namespace HvZ.Networking

open HvZ.Common
open System.Net
open System.Net.Sockets

[<AutoOpen>]
module Internal =
   open HvZ.Common
   open System.Text
   open System.Text.RegularExpressions
   (*
   let smoosh (xs : byte[]) =
      let len = xs.Length / 2 + xs.Length % 2
      let arr = Array.zeroCreate len
      for i = 0 to arr.Length-1 do
         arr.[i] <- (xs.[i*2] <<< 4) ||| (if i = arr.Length-1 && xs.Length % 2 = 1 then 0uy else xs.[i*2+1])
      System.Convert.ToBase64String arr

   let unsmoosh xs =
      seq {
         for x in System.Convert.FromBase64String xs do
            yield x >>> 4
            yield x &&& 0xFuy
      } |> Seq.toArray
   *)
   let toProtocol cmd =
      let s =
         match cmd with
         | Forward (wId, dist) -> sprintf "forward %d %.2f" wId dist
         | Left (wId, degrees) -> sprintf "left %d %.2f" wId degrees
         | Right (wId, degrees) -> sprintf "right %d %.2f" wId degrees
         | Eat wId -> sprintf "eat %d" wId
         | Bite (wId, target) -> sprintf "bite %d %d" wId target
         | TakeFood (wId, fromWhere) -> sprintf "takefood %d %d" wId fromWhere
         | TakeSocks (wId, fromWhere) -> sprintf "takesocks %d %d" wId fromWhere
         | Throw (wId, heading) -> sprintf "throw %d %.2f" wId heading
         | ZombieJoin (gameId, name) ->
            if name = null || name.Length = 0 then failwith "No name supplied for this Zombie"
            else sprintf "zjoin %s %s" gameId name
         | HumanJoin (gameId, name) ->
            if name = null || name.Length = 0 then failwith "No name supplied for this Human"
            else sprintf "hjoin %s %s" gameId name
         | Hello playerId -> sprintf "hello %d" playerId
         | Create mapdata -> sprintf "create %s" mapdata
         | CreateOK gameId -> sprintf "createok %s" gameId
         | Game gameId -> sprintf "game %s" gameId
         | ListStart -> sprintf "begin"
         | ListEnd -> sprintf "end"
         | Human (walkerId, x, y, heading, name) -> sprintf "human %d %.2f %.2f %.2f %s" walkerId x y heading name
         | Zombie (walkerId, x, y, heading, name) -> sprintf "zombie %d %.2f %.2f %.2f %s" walkerId x y heading name
         | Move -> "move"
         | No why -> sprintf "no %s" why // reason for rejection
      let byteCount = Encoding.UTF8.GetByteCount s
      if byteCount > 99999 then
         failwith "Sorry, the data you're sending the server is too large.  I won't send it."
      else
         Encoding.UTF8.GetBytes (sprintf "%5d%s" byteCount s)

   let fromString =
      let makeMatcher reString (f : _[] -> Command) = 
         let re = Regex(reString, RegexOptions.Compiled ||| RegexOptions.IgnoreCase ||| RegexOptions.Singleline)
         re, f
      let matchers = [|
         makeMatcher @"^forward (\d+) (\d+\.\d{2})$" (fun m -> Forward(uint32 m.[1], float m.[2]))
         makeMatcher @"^left (\d+) (\d+\.\d{2})$" (fun m -> Left(uint32 m.[1], float m.[2]))
         makeMatcher @"^right (\d+) (\d+\.\d{2})$" (fun m -> Right(uint32 m.[1], float m.[2]))
         makeMatcher @"^eat (\d+)$" (fun m -> Eat(uint32 m.[1]))
         makeMatcher @"^bite (\d+) (\d+)$" (fun m -> Bite(uint32 m.[1], uint32 m.[2]))
         makeMatcher @"^takefood (\d+) (\d+)$" (fun m -> TakeFood(uint32 m.[1], uint32 m.[2]))
         makeMatcher @"^takesocks (\d+) (\d+)$" (fun m -> TakeSocks(uint32 m.[1], uint32 m.[2]))
         makeMatcher @"^throw (\d+) (\d+\.\d{2})$" (fun m -> Throw(uint32 m.[1], float m.[2]))
         makeMatcher @"^zjoin (.{20}) (.+)$" (fun m -> ZombieJoin(m.[1], m.[2]))
         makeMatcher @"^hjoin (.{20}) (.+)$" (fun m -> HumanJoin(m.[1], m.[2]))
         makeMatcher @"^hello (\d+)$" (fun m -> Hello(uint32 m.[1]))
         makeMatcher @"^create (.+)$" (fun m -> Create(m.[1]))
         makeMatcher @"^createok (.{20})$" (fun m -> CreateOK(m.[1]))
         makeMatcher @"^game (.{20})$" (fun m -> Game(m.[1]))
         makeMatcher @"^begin$" (fun _ -> ListStart)
         makeMatcher @"^end$" (fun _ -> ListEnd)
         makeMatcher @"^human (\d+) (\d+\.\d{2}) (\d+\.\d{2}) (\d+\.\d{2}) (.+)$" (fun m -> Human(uint32 m.[1], float m.[2], float m.[3], float m.[4], m.[5]))
         makeMatcher @"^zombie (\d+) (\d+\.\d{2}) (\d+\.\d{2}) (\d+\.\d{2}) (.+)$" (fun m -> Zombie(uint32 m.[1], float m.[2], float m.[3], float m.[4], m.[5]))
         makeMatcher @"^move$" (fun _ -> Move)
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
      let buffer = Array.zeroCreate (100 * 1024) // enough space for any 5-digit messages.
      let stream = client.GetStream ()
      MailboxProcessor.Start(fun inbox ->
         let rec receive offset =
            async {
               let! n = stream.AsyncRead (buffer, offset, buffer.Length-offset)
               //printfn "Received %d bytes" n
               match n with
               | 0 -> onClosed StreamClosed // shutdown.
               | n when n+offset < 5 -> do! receive (n+offset) // trickling? Ah, well.  Keep receiving.
               | _ ->
                  let rec parseCommands offset remaining =
                     if remaining >= 5 then
                        match safeGetInt (Array.sub buffer offset 5) with
                        | None -> Corrupt
                        | Some n when n < 0 -> Corrupt
                        | Some expectedLen ->
                           if remaining-5 >= expectedLen then // ok, received it all.
                              let s = safeGetString (Array.sub buffer (offset+5) expectedLen)
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
                                 parseCommands (offset+expectedLen+5) (remaining-(expectedLen+5))
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

namespace HvZ.AI
open HvZ.Common

type IHumanPlayer =
   inherit IWalker
   abstract member GoForward: distance:float -> unit
   abstract member TurnLeft: degrees:float -> unit
   abstract member TurnRight: degrees:float -> unit
   abstract member Eat: unit -> unit
   abstract member TakeFoodFrom: place:IIdentified -> unit
   abstract member TakeSocksFrom: place:IIdentified -> unit
   abstract member Throw: heading:float -> unit
   abstract member MapWidth : float with get
   abstract member MapHeight : float with get

type IZombiePlayer =
   inherit IWalker
   abstract member GoForward: distance:float -> unit
   abstract member TurnLeft: degrees:float -> unit
   abstract member TurnRight: degrees:float -> unit
   abstract member Eat: target:IIdentified -> unit
   abstract member MapWidth : float with get
   abstract member MapHeight : float with get

namespace HvZ.Common
open HvZ.Networking
open System.Net.Sockets

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
   member __.CreateGame mapData = send () (Create mapData)
   member __.PlayerId with get () = Option.get playerId