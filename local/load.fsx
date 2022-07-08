open System.Threading.Tasks

#r "nuget: Dapr.Client"
#r "nuget: Dapr.Actors"
#r "nuget: Dapr.Extensions.Configuration"
#r "nuget: Ply"

#r "../projects/actors/bin/Debug/net6.0/Actors.App.dll"


open FSharp.Control.Tasks
open Places
open System


let refillN (place: IPlace) n =
    [| 1 .. n |]
    |> Array.map (fun t -> place.Refill t :> Task)
    |> Task.WaitAll

let toString i = i.ToString()


let numberPlaces = 2_000
let numberRefills = 50




task {
    // let b0 = getPlaceProxy "b0"
    // let b1 = getPlaceProxy "b1"
    // let b2 = getPlaceProxy "b2"
    // do! b1.AddParent "b0"
    // do! b2.AddParent "b1"

    // refillN b0 numberRefills

    // let! quantity = b0.Status()
    // printf "quantiy b0: %A" quantity

    // let! quantity = b1.Status()
    // printf "quantiy b1: %A" quantity

    // let! quantity = b2.Status()
    // printf "quantiy b2: %A" quantity
    return ()
}



task {

    // performance
    let timer = Diagnostics.Stopwatch()
    timer.Start()

    let places =
        [| 1 .. numberPlaces |]
        |> Array.map (toString >> getPlaceProxy)

    for place in places do
        refillN place numberRefills

    printfn "Elapsed Time: %i" timer.ElapsedMilliseconds

    return ()
}
