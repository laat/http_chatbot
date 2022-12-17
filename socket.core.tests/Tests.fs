module Tests

open System
open Xunit
open socket.core.TcpWrappers
open System.Threading
open System.Net
open System.IO

open ConnectionController 

type FakeClientWrapper () = 
   let mutable ms = new MemoryStream()
   interface ITcpClient with
       member this.Close(): unit = ()
       member this.GetStream(): IO.Stream = 
           ms 
   interface IDisposable with
       member this.Dispose() =
           ms.Dispose()

type FakeTcpListener(delay: int) =
   interface ITcpListener with
       member this.Stop(): unit = 
              ()
       member this.AcceptTcpClientAsync(ct: CancellationToken): Async<ITcpClient*IPEndPoint> = 
              async {
                  do! Async.Sleep(delay) 
                  if (ct.IsCancellationRequested) then failwith "fooobar"
                  return (new FakeClientWrapper(), new IPEndPoint(1,1)) 
              }

       member this.Start(): unit = () 
 
[<Fact>]
let ``Connection controller returns a connection`` () =
    let cts = new CancellationTokenSource()
    let fakeListener = FakeTcpListener(0) 
    let server = ListenConnections (fakeListener, cts.Token) 
    let reply = server.PostAndReply(fun channel -> (GetNewConnection channel))
    match reply with 
                    | Some (_, endpoint) -> Assert.Equal(1, endpoint.Port)
                    | _ -> failwith "fooo" 
    cts.Cancel()

[<Fact>]
let ``Connection failed to request within time returns none`` () =
    let cts = new CancellationTokenSource()
    let fakeListener = FakeTcpListener(20) 
    let server = ListenConnections (fakeListener, cts.Token) 
    let reply = server.PostAndReply(fun channel -> (GetNewConnection channel))
    match reply with 
                    | Some (_, _) -> failwith "should not be anything" 
                    | None  -> Assert.True(true) 
    cts.Cancel()

[<Fact>]
let ``Connection stopped`` () =
    let cts = new CancellationTokenSource()
    cts.Cancel()
    let fakeListener = FakeTcpListener(20) 
    let server = ListenConnections (fakeListener, cts.Token) 
    let reply = server.PostAndReply(fun channel -> (GetNewConnection channel))
    match reply with 
                    | Some (_, _) -> failwith "should not be anything" 
                    | None  -> Assert.True(true) 
