namespace WebSocketApp

open System.Threading.Tasks

module Middleware =
    open System
    open System.Text
    open System.Threading
    open System.Net.WebSockets
    open Microsoft.AspNetCore.Http

    open FSharp.Control.Tasks

    let mutable sockets = list<WebSocket>.Empty

    let private addSocket sockets socket = socket :: sockets

    let private removeSocket sockets socket =
        sockets
        |> List.choose (fun s -> if s <> socket then Some s else None)

    let private sendMessage =
        fun (socket: WebSocket) (message: string) ->
            task {
                printfn "sendMessage"
                let buffer = Encoding.UTF8.GetBytes(message)
                let segment = new ArraySegment<byte>(buffer)

                if socket.State = WebSocketState.Open then
                    do! socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None)
                else
                    sockets <- removeSocket sockets socket
            }

    let sendMessageToSockets =
        fun message ->
            task {
                printfn "sendMessageToSockets"

                for socket in sockets do
                    try
                        do! sendMessage socket message
                    with
                    | _ -> sockets <- removeSocket sockets socket
            }

    type WebSocketMiddleware(next: RequestDelegate) =
        member __.Invoke(ctx: HttpContext) =
            task {
                if ctx.Request.Path = PathString("/ws") then
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()


                        sockets <- addSocket sockets webSocket

                        let buffer: byte [] = Array.zeroCreate 4096
                        let! ct = Async.CancellationToken

                        do! webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct) :> Task
                    // ctx.Response.StatusCode <- 200

                    | false -> ctx.Response.StatusCode <- 400
                else
                    return! next.Invoke(ctx)
            }
