module logger.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open WebSocketApp.Middleware
open FSharp.Control.Tasks


// ---------------------------------
// Models
// ---------------------------------

type Message = { Text: string }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title [] [ encodedText "logger" ]
                link [ _rel "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] [
                div [ _id "app" ] [
                    ul [ attr "m-for" "message in messages" ] [
                        li [] [ rawText "{{ message }}" ]
                    ]
                ]
            ]
            script [ _src "https://cdnjs.cloudflare.com/ajax/libs/moonjs/0.11.0/moon.min.js" ] []
            script [ _type "application/javascript" ] [
                rawText
                    """
                    function openWebSocket(vm) {
                        var loc = window.location;
                        let wsUri = '';
                        if (loc.protocol === "https:") {
                            wsUri = "wss:";
                        } else {
                            wsUri = "ws:";
                        }
                        wsUri += "//" + loc.host;
                        wsUri += loc.pathname + "ws";
                        var socket = new WebSocket(wsUri)
                        socket.onopen = function () {
                            console.log('INFO: WebSocket opened successfully');
                        }
                        socket.onclose = function () {
                            console.log('INFO: WebSocket closed');
                            openWebSocket(vm);
                        }
                        socket.onmessage = function (messageEvent) {
                            var messages = vm.get('messages');
                            messages.unshift(messageEvent.data);
                            vm.set('messages', messages);
                        }
                        vm.set('socket', socket);
                    }
                    const app = new Moon({
                        el: '#app',
                        data: {
                            messages: []
                        },
                        hooks: {
                            init: function () {
                                var vm = this;
                                openWebSocket(vm);
                            }
                        }
                    });
                    """
            ]
        ]

    let partial () = h1 [] [ encodedText "logger" ]

    let index (model: Message) =
        [ partial ()
          p [] [ encodedText model.Text ] ]
        |> layout

// ---------------------------------
// Web app
// ---------------------------------

let indexHandler (name: string) =
    let greetings = sprintf "Hello %s, from Giraffe!" name
    let model = { Text = greetings }
    let view = Views.index model
    htmlView view

let receiveMessage: HttpHandler =
    handleContext
        (fun ctx ->
            task {
                let! msg = ctx.ReadBodyFromRequestAsync()
                do! sendMessageToSockets msg
                return! ctx.WriteTextAsync ""
            })

let webApp =
    choose [ GET
             >=> choose [ route "/" >=> indexHandler "world"
                          routef "/hello/%s" indexHandler ]
             POST
             >=> choose [ route "/notification" >=> receiveMessage ]
             setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")

    clearResponse
    >=> setStatusCode 500
    >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder: CorsPolicyBuilder) =
    builder
        .WithOrigins("http://localhost:5557", "https://localhost:5557")
        .AllowAnyMethod()
        .AllowAnyHeader()
    |> ignore

let configureApp (app: IApplicationBuilder) =
    let env =
        app.ApplicationServices.GetService<IWebHostEnvironment>()

    (match env.IsDevelopment() with
     | true -> app.UseDeveloperExceptionPage()
     | false -> app.UseGiraffeErrorHandler(errorHandler))
        // .UseCors(configureCors)
        .UseWebSockets()
        .UseMiddleware<WebSocketMiddleware>()
        .UseStaticFiles()
        .UseCloudEvents()
        // .UseEndpoints(fun endoptions -> endoptions.MapSubscribeHandler() |> ignore)
        .UseGiraffe(
            webApp
        )

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")

    Host
        .CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .UseContentRoot(contentRoot)
                .UseWebRoot(webRoot)
                .UseUrls("http://*:5557")
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
            |> ignore)
        .Build()
        .Run()

    0
