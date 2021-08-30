module Places

open Dapr.Actors
open Dapr.Actors.Runtime
open FSharp.Control.Tasks
open System.Threading.Tasks
open System
open Dapr.Client
open Dapr.Actors.Client
open FsToolkit.ErrorHandling
open FSharpPlus

type PlaceState =
    { Name: string option
      Quantity: int
      ChildsQuantity: int
      Parent: string option }
    static member Zero() : PlaceState =
        { Name = None
          Quantity = 0
          ChildsQuantity = 0
          Parent = None }

// reminder for a future me the actor actions must return a Task or Task<'T>
type IPlace =
    inherit IActor
    abstract member Refill : quantity: int -> Task<int>
    abstract member RefillFromChild : quantity: int -> origin: string -> Task<int>
    abstract member Discharge : quantity: int -> Task<int>
    abstract member DischargeFromChild : quantity: int -> origin: string -> Task<int>
    abstract member Status : unit -> Task<PlaceState>
    abstract member AddParent : parent: string -> Task

type Refilled =
    { Id: string
      QuantityFilled: int
      CurrrentQunatiy: int }

type Discharged =
    { Id: string
      QuantityDischarged: int
      CurrrentQunatiy: int }

type Event =
    | Refilled
    | Discharged

let stateName = "state"

let getStringTime () =
    DateTime
        .Now
        .ToUniversalTime()
        .ToString("yyyyMMddHHmmssffff")

let getPlaceProxy id =
    ActorProxy.Create<IPlace>(id |> ActorId, "PlaceActor")

type PlaceActor(host, dapr: DaprClient) =
    inherit Actor(host)
    let mutable state = PlaceState.Zero()
    let stateManager = base.StateManager
    let log = base.Logger
    let proxy = base.ProxyFactory
    let id = base.Id.ToString()
    let client = dapr

    let addParent parent =
        task {
            let newState = { state with Parent = Some parent }
            state <- newState
            return newState
        }

    let refill getNow quantity origin =
        let newState =
            { state with
                  Quantity = state.Quantity + quantity
                  ChildsQuantity =
                      if id = origin then
                          state.ChildsQuantity
                      else
                          state.ChildsQuantity + quantity }

        let event: Refilled =
            { Id = origin
              QuantityFilled = quantity
              CurrrentQunatiy = newState.Quantity }

        task {
            let storePath =
                $"events||{getNow ()}||{nameof event}"
                |> String.toLower

            do! stateManager.SetStateAsync(storePath, event)

            do!
                match state.Parent with
                | None -> Task.CompletedTask
                | Some parentId ->
                    task {
                        let parent = getPlaceProxy parentId
                        return! parent.RefillFromChild quantity origin
                    }
                    :> Task

            do! client.PublishEventAsync("pubsub", "notification", event)
            do! stateManager.SetStateAsync(stateName, newState)
            return newState
        }
        |> Task.map
            (fun newState ->
                state <- newState
                newState.Quantity)

    let discharge getNow quantity origin =
        let newState =
            { state with
                  Quantity = state.Quantity - quantity
                  ChildsQuantity =
                      if id = origin then
                          state.ChildsQuantity
                      else
                          state.ChildsQuantity + quantity }

        let event: Discharged =
            { Id = origin
              QuantityDischarged = quantity
              CurrrentQunatiy = newState.Quantity }

        task {
            do! stateManager.SetStateAsync($"events||{getNow ()}||discharged", event)

            do!
                match state.Parent with
                | None -> Task.CompletedTask
                | Some parentId ->
                    task {
                        let parent = getPlaceProxy parentId
                        return! parent.DischargeFromChild quantity origin
                    }
                    :> Task

            do! client.PublishEventAsync("pubsub", "notification", event)
            do! stateManager.SetStateAsync(stateName, newState)
            return newState
        }
        |> Task.map
            (fun newState ->
                state <- newState
                newState.Quantity)


    override _.OnActivateAsync() =

        task {
            let! currentState = stateManager.GetOrAddStateAsync(stateName, state)
            state <- currentState
            printf $"OnActivateAsync {state}"
        }
        :> Task

    override _.OnDeactivateAsync() =
        task { do! stateManager.SetStateAsync(stateName, state) } :> Task

    interface IPlace with
        member _.Refill quantity = refill getStringTime quantity id
        member _.RefillFromChild quantity origin = refill getStringTime quantity origin

        member _.Discharge quantity = discharge getStringTime quantity id

        member _.DischargeFromChild quantity origin = discharge getStringTime quantity origin

        member _.AddParent parent = addParent parent :> Task
        member _.Status() = Task.FromResult state
