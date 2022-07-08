module Sub.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks
open Giraffe

open Dapr.Client
open Dapr.Extensions.Configuration
open Places


let darp = DaprClientBuilder().Build()

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
                title [] [ encodedText "sub" ]
                link [ _rel "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial () = h1 [] [ encodedText "sub" ]

    let index (model: Message) =
        [ partial ()
          p [] [ encodedText model.Text ] ]
        |> layout

// ---------------------------------
// Web app
// ---------------------------------

let indexHandler (name: string) =
    let greetings =
        sprintf "Hello secret %s, from Giraffe!" name

    let model = { Text = greetings }
    let view = Views.index model
    htmlView view


let indexWithSecret =
    handleContext
        (fun ctx ->
            task {
                let settings = ctx.GetService<IConfiguration>()
                let darp = ctx.GetService<DaprClient>()
                // let log = ctx.GetService<ILogger>()

                let name = settings.Item("super-secret")

                let greetings = sprintf "Hello secret %s!" name

                do! darp.SaveStateAsync("statestore", "name", name)
                printf $"save name in db \"{name}\""
                let model = { Text = greetings }
                let view = Views.index model
                return! ctx.WriteHtmlViewAsync view
            })


let webApp =
    choose [ GET
             >=> choose [ route "/" >=> indexWithSecret
                          routef "/hello/%s" indexHandler ]
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
        .WithOrigins("http://localhost:5556", "https://localhost:5556")
        .AllowAnyMethod()
        .AllowAnyHeader()
    |> ignore

let configureApp (app: IApplicationBuilder) =
    let env =
        app.ApplicationServices.GetService<IWebHostEnvironment>()

    (match env.IsDevelopment() with
     | true -> app.UseDeveloperExceptionPage()
     | false -> app.UseGiraffeErrorHandler(errorHandler))
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseRouting()
        .UseEndpoints(fun endoptions -> endoptions.MapActorsHandlers() |> ignore)
        .UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    services.AddSingleton(darp) |> ignore

    services.AddActors
        (fun options ->
            options.Actors.RegisterActor<PlaceActor>()
            printf "%A" options.Actors.Count)
    |> ignore


let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

let configureAppConfiguration (configureBuilder: IConfigurationBuilder) =
    configureBuilder.AddDaprSecretStore("my-secret-store", darp)
    |> ignore

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
                .UseUrls("http://localhost:5556")
                .Configure(Action<IApplicationBuilder> configureApp)
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
                .ConfigureAppConfiguration(configureAppConfiguration)
            |> ignore)
        .Build()
        .Run()

    0
