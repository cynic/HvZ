namespace HvZ

module internal Mxyzptlk =
   [<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("HvZCommon")>]
   [<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("HvZ")>]
   [<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("HvZServer")>]
   do ()

type IIdentified =
   abstract member Id : uint32 with get

type SupplyItem =
| Food = 0
| Sock = 1

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

   override __.ToString () = sprintf "(%.2f, %.2f)" __.X __.Y

type ITakeSpace =
   abstract member Position : Position with get
   abstract member Radius : float with get

type MoveState =
| Stopped = 0
| Moving = 1

type IWalker =
   inherit ITakeSpace
   /// <summary>heading is in degrees, 0 is directly upwards</summary>
   abstract member Heading : float with get
   abstract member Name : string with get
   abstract member Lifespan : int with get
   abstract member MaximumLifespan : int with get
   abstract member Movement : MoveState with get

namespace HvZ.Common
open HvZ

(*
How to add new commands:

1. Add a line for the command (including the parameters for it), to the Command type.
2. Add a line for the command to the 'toProtocol' function.
3. Add a line for the command to the 'fromString' function.
4. (only necessary if the Client needs to send this command) Add a member to the HvZConnection class.

aaand, that's it.  You've now successfully got a Brand Spanking New valid protocol command.  Congratulations.
*)

type internal Command =
| Forward of uint32 * float // walkerId * distance
| Turn of uint32 * float // walkerId * degrees
| Eat of uint32 // walkerId
| Bite of uint32 * uint32 // biter * bitten
| TakeFood of uint32 * uint32 // who-takes-it * taken-from-where
| TakeSocks of uint32 * uint32 // who-takes-it * taken-from-where
| Throw of uint32 * float // walkerId * heading
| ZombieJoin of string * string * string // b64gameId * correlator-guid * name * mapData
| HumanJoin of string * string * string  // b64gameId * correlator-guid * name * mapData
| Create of string * string // game-name * mapdata
| Games of string[] // b64gameId[]
| JoinOK of uint32 * string * string // walkerId * correlator-guid * mapData
| Move
| GameNo of string // reason for rejection (only sent before joining has occurred, OR for events that affect every player)
| No of uint32 * string // walkerId * reason for rejection
with
   override __.ToString () =
      match __ with
      | Forward (_,d) -> sprintf "walk forward %f units" d
      | Turn (_,d) -> sprintf "turn by %f degrees" d
      | Eat _ -> "eat food"
      | Bite _ -> "bite a human"
      | TakeFood _ -> "take some food from a resupply-point"
      | TakeSocks _ -> "take some socks from a resupply-point"
      | Throw _ -> "throw some socks"
      | ZombieJoin (_,guid,name) -> sprintf "join the game as a zombie named %s (guid=%s)" name guid
      | HumanJoin (_,guid,name) -> sprintf "join the game as a human named %s (guid=%s)" name guid
      | JoinOK _ -> sprintf "accept a request to join the game"
      | Create _ -> "create a game"
      | GameNo reason | No (_, reason) -> reason
      | Games xs -> sprintf "here is a list of %d different games" xs.Length
      | Move -> "make a move in the game"

type internal ICommandInterpreter =
   abstract member Forward : walkerId:uint32 -> distance:float -> unit
   abstract member Turn : walkerId:uint32 -> degrees:float -> unit
   abstract member Eat : walkerId:uint32 -> unit
   abstract member Bite : walkerId:uint32 -> target:uint32 -> unit
   abstract member TakeFood : walkerId:uint32 -> resupplyId:uint32 -> unit // who-takes-it -> taken-from-where
   abstract member TakeSocks : walkerId:uint32 -> resupplyId:uint32 -> unit // who-takes-it -> taken-from-where
   abstract member Throw : walkerId:uint32 -> heading:float -> unit // walkerId -> heading
   abstract member JoinOK : walkerId:uint32 -> guid:string -> mapData:string -> unit
   abstract member Move : unit -> unit
   abstract member GameNo : reason:string -> unit // reason for rejection
   abstract member No : walkerId:uint32 -> reason:string -> unit // walkerId -> reason for rejection

[<Extension>]
type internal Command with
   static member Dispatch cmd (x : ICommandInterpreter) =
      match cmd with
      | Forward (wId, dist) -> x.Forward wId dist
      | Turn (wId, degrees) -> x.Turn wId degrees
      | Eat wId -> x.Eat wId
      | TakeFood (wId, fromWhere) -> x.TakeFood wId fromWhere
      | TakeSocks (wId, fromWhere) -> x.TakeSocks wId fromWhere
      | Throw (wId, heading) -> x.Throw wId heading
      | Bite (wId, target) -> x.Bite wId target
      | JoinOK (walkerId, guid, mapData) -> x.JoinOK walkerId guid mapData
      | Move -> x.Move ()
      | GameNo why -> x.GameNo why
      | No (walkerId, why) -> x.No walkerId why
      | _ -> printfn "I shouldn't be receiving %A commands..." cmd // ignore?

type internal ClientStatus =
| InGame of string // gameId
| OutOfGame

namespace HvZ.AI
open HvZ
open HvZ.Common

type IHumanPlayer =
   inherit IWalker
   abstract member GoForward: distance:float -> unit
   abstract member Turn: degrees:float -> unit
   abstract member Eat: unit -> unit
   abstract member TakeFoodFrom: place:IIdentified -> unit
   abstract member TakeSocksFrom: place:IIdentified -> unit
   abstract member Throw: heading:float -> unit
   abstract member MapWidth : float with get
   abstract member MapHeight : float with get
   abstract member Inventory : SupplyItem[] with get
   abstract member Movement : MoveState with get

type IZombiePlayer =
   inherit IWalker
   abstract member GoForward: distance:float -> unit
   abstract member Turn: degrees:float -> unit
   abstract member Eat: target:IIdentified -> unit
   abstract member MapWidth : float with get
   abstract member MapHeight : float with get
   abstract member Movement : MoveState with get

namespace HvZ.Networking

open HvZ.Common
open System.Net
open System.Net.Sockets

[<AutoOpen>]
module internal Internal =
   open HvZ.Common
   open System.Text
   open System.Text.RegularExpressions

   let internal nextStringId =
      let s = Seq.initInfinite (fun _ -> Array.sub (System.Guid.NewGuid().ToByteArray()) 0 15 |> System.Convert.ToBase64String)
      let iter = s.GetEnumerator()
      fun () ->
         lock (s) (fun () ->
            ignore <| iter.MoveNext()
            iter.Current
         )

   let internal nextUIntId =
      let v = ref 0
      fun () ->
         System.Threading.Interlocked.Increment v |> uint32

   let internal toBase64 (s : string) = System.Text.Encoding.UTF8.GetBytes s |> System.Convert.ToBase64String
   let internal fromBase64 (s : string) = System.Convert.FromBase64String s |> System.Text.Encoding.UTF8.GetString

   let internal toProtocol cmd =
      let s =
         match cmd with
         | Forward (wId, dist) -> sprintf "forward %d %.2f" wId dist
         | Turn (wId, degrees) -> sprintf "turn %d %.2f" wId degrees
         | Eat wId -> sprintf "eat %d" wId
         | Bite (wId, target) -> sprintf "bite %d %d" wId target
         | TakeFood (wId, fromWhere) -> sprintf "takefood %d %d" wId fromWhere
         | TakeSocks (wId, fromWhere) -> sprintf "takesocks %d %d" wId fromWhere
         | Throw (wId, heading) -> sprintf "throw %d %.2f" wId heading
         | ZombieJoin (gameId, guid, name) ->
            if name = null || name.Length = 0 then failwith "No name supplied for this Zombie"
            if name.Length > 500 then failwith "Name of Zombie is too long"
            sprintf "zjoin %s %s %s" (toBase64 gameId) guid name
         | HumanJoin (gameId, guid, name) ->
            if name = null || name.Length = 0 then failwith "No name supplied for this Human"
            if name.Length > 500 then failwith "Name of Human is too long"
            sprintf "hjoin %s %s %s" (toBase64 gameId) guid name
         | Create (gameName, mapData) -> sprintf "create %s %s" (toBase64 gameName) mapData
         | Games gameIds ->
            let s = gameIds |> Array.map toBase64 |> String.concat " "
            sprintf "game %s" s
         | JoinOK (walkerId, guid, mapData) -> sprintf "joinok %d %s %s" walkerId guid mapData
         | Move -> "move"
         | GameNo why -> sprintf "gameno %s" why
         | No (walkerId, why) -> sprintf "no %d %s" walkerId why // reason for rejection
      let msgId = nextUIntId ()
      let byteCount = Encoding.UTF8.GetByteCount s
      if byteCount > 99999 then
         failwith "Sorry, the data you're sending the server is too large.  I won't send it."
      else
         Encoding.UTF8.GetBytes (sprintf "%5d%s" byteCount s)

   let internal fromString =
      let makeMatcher reString (f : _[] -> Command) = 
         let re = Regex(reString, RegexOptions.Compiled ||| RegexOptions.IgnoreCase ||| RegexOptions.Singleline)
         re, f
      let matchers = [|
         makeMatcher @"^forward (\d+) (\d{0,6}\.\d{2})$" (fun m -> Forward(uint32 m.[1], float m.[2]))
         makeMatcher @"^turn (\d+) (-?\d{0,5}\.\d{2})$" (fun m -> Turn(uint32 m.[1], float m.[2]))
         makeMatcher @"^eat (\d+)$" (fun m -> Eat(uint32 m.[1]))
         makeMatcher @"^bite (\d+) (\d+)$" (fun m -> Bite(uint32 m.[1], uint32 m.[2]))
         makeMatcher @"^takefood (\d+) (\d+)$" (fun m -> TakeFood(uint32 m.[1], uint32 m.[2]))
         makeMatcher @"^takesocks (\d+) (\d+)$" (fun m -> TakeSocks(uint32 m.[1], uint32 m.[2]))
         makeMatcher @"^throw (\d+) (\d+\.\d{2})$" (fun m -> Throw(uint32 m.[1], float m.[2]))
         makeMatcher @"^zjoin ([^ ]{1,100}) ([^ ]+) (.{1,500})$" (fun m -> ZombieJoin(fromBase64 m.[1], m.[2], m.[3]))
         makeMatcher @"^hjoin ([^ ]{1,100}) ([^ ]+) (.{1,500})$" (fun m -> HumanJoin(fromBase64 m.[1], m.[2], m.[3]))
         makeMatcher @"^create ([^ ]{1,100}) (.{1,99500})$" (fun m -> Create(fromBase64 m.[1], m.[2]))
         makeMatcher @"^game (.+)$" (fun m -> Games(m.[1].Split(' ') |> Array.map fromBase64))
         makeMatcher @"^joinok (\d+) ([^ ]+) (.+)$" (fun m -> JoinOK(uint32 m.[1], m.[2], m.[3]))
         makeMatcher @"^move$" (fun _ -> Move)
         makeMatcher @"^gameno (.+)$" (fun m -> GameNo(m.[1]))
         makeMatcher @"^no (\d+) (.+)$" (fun m -> No(uint32 m.[1], m.[2]))
      |]
      fun txt ->
         matchers |> Array.tryPick (fun (re, f) ->
            let m = re.Match txt
            if m.Success then
               try
                  let args = m.Groups |> Seq.cast<Group> |> Seq.map (fun x -> x.Value) |> Seq.toArray
                  Some (f args)
               with
               | e ->
                  printfn "Exception while decoding message: %A" e
                  None // hmm, nope.
            else None
         )

   let internal writeTo (s : System.IO.Stream) c =
      let arr = toProtocol c
      s.Write (arr, 0, arr.Length)

   type private ParseCommandResult =
   | Corrupt
   | OK of int // int = offset for new command reception
   
   type internal ShutdownReason =
   | CorruptStream
   | StreamClosed

   let private safeGetString buf =
      try
         Some (Encoding.UTF8.GetString buf)
      with
      | _ -> None

   let private safeGetInt str =
      safeGetString str
      |> Option.bind (fun s ->
         System.Int32.TryParse s
         |> fun (ok, i) -> if ok then Some i else None
      )

   let internal connection (client : TcpClient) onClosed onCommandReceived onAbnormalCommand =
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

   let internal serverHandleTcp (client : TcpClient) handleRequest =
      let stream = client.GetStream()
      let clientId = string client.Client.RemoteEndPoint
      let connId = nextStringId ()
      let onClosed = function
         | CorruptStream -> printfn "Corrupt stream from %s quitting." clientId
         | StreamClosed -> printfn "Connection to %s closed" clientId
      let onCommandReceived cmd stream =
         handleRequest connId cmd (fun x -> writeTo stream x)
      let onAbnormalCommand s stream =
         GameNo (sprintf "I couldn't understand a command that an AI on this computer sent (%s)" s) |> writeTo stream
      connection client onClosed onCommandReceived onAbnormalCommand

   let internal clientHandleTcp (client : TcpClient) onClosed onCommandReceived =
      let stream = client.GetStream()
      let connId = nextStringId ()
      let onClosed = function
         | CorruptStream -> failwith "There was an error on the server - ask your lecturer to look into it.  Sorry."
         | StreamClosed -> onClosed () // Goodbye, quite naturally.
      let onAbnormalCommand s _ =
         failwithf "The server sent me a weird command (%s).  Tell your lecturer." s
      connection client onClosed onCommandReceived onAbnormalCommand
      |> ignore

namespace HvZ.Common
open HvZ.Networking
open System.Net.Sockets

type internal CommandEventArgs(command : Command) =
   inherit System.EventArgs()
   member __.Command with get () = command
       
type internal HvZConnection() as this =
   let dataEvent = new Event<System.EventHandler<CommandEventArgs>,CommandEventArgs>()
   let closedEvent = new Event<_>()
   let mutable conn : TcpClient option = None
   let onClosed () = closedEvent.Trigger ()
   let onCommandReceived cmd stream = 
      match cmd with
      | Forward _ | Turn _ | Move -> ()
      | _ -> eprintfn "Server said: %A" cmd
      dataEvent.Trigger (this, new CommandEventArgs(cmd))
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
   [<CLIEvent>]
   member __.OnCommandReceived = dataEvent.Publish
   [<CLIEvent>]
   member __.OnConnectionClosed = closedEvent.Publish
   interface System.IDisposable with
      member __.Dispose () =
         match conn with
         | Some conn ->
            (conn :> System.IDisposable).Dispose ()
         | None -> ()

   (* Here be members which hide the nasty details of Command interop *)
   member __.CreateGame mapData = send () (Create mapData)