namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open Fable.PowerPack

open System

module Main =

  type Behavior = Func<ESprite, float, JS.Promise<bool>>

  and ESprite(t: Texture, id: string, behaviors: Behavior list) =
    inherit Sprite(t)
    let mutable _behaviors = behaviors
    let mutable _disposed = false
    let mutable _prevTime = 0.

    member self.Id = id
    member self.IsDisposed = _disposed

    member self.AddBehavior(b:Behavior) =
      _behaviors <- b :: _behaviors

    member self.Update(dt: float) =
      promise {
        let behaviors = _behaviors
        _behaviors <- []
        let mutable notCompletedBehaviors = []
        let dt =
          let tmp = _prevTime
          _prevTime <- dt
          if tmp = 0. then 0. else dt - tmp
        for b in behaviors do
          let! complete = b.Invoke(self, dt)
          if not complete then
            notCompletedBehaviors <- b :: notCompletedBehaviors
        _behaviors <- _behaviors @ notCompletedBehaviors
      }

    interface IDisposable with
      member self.Dispose() =
        if not _disposed then
          _disposed <- true
          self.parent.removeChild(self) |> ignore

  and State =
    | Nothing
    | Loading
    | Play
    | MainTitle

  module Behaviors =
    let moveable = ()

  open Behaviors

  let updateLoop (RendererOptions: WebGLRenderer) (stage:Container) =

    let fps = 60.
    let mutable state : State = Loading
    let mutable id = -1
    let mutable resources  = Unchecked.defaultof<loaders.ResourceDictionary>
    let mutable sprites = []


    let nextId () =
      id <- id + 1
      sprintf "%i" id
    let makeESprite (behaviors: Behavior list) (t: Texture) =
      new ESprite(t, nextId(), behaviors)
    let getTexture name =
      resources.Item(name).texture
    let setPosition x y (s: Sprite) =
      s.position <- Point(x, y)
      s
    let drawRect (g:Graphics) color width height =
      g.beginFill(float color) |> ignore
      g.drawRect(0., 0., width, height) |> ignore
      g.endFill() |> ignore
      g
    let addToESprites (s: ESprite) =
      sprites <- [s] @ sprites
      s

    let update (currentState: State) =

      match currentState with
      | Nothing -> State.Nothing
      | Loading ->

          Browser.console.log "cojujio"

          let onLoadComplete r =
            resources <- unbox<loaders.ResourceDictionary> r
            Browser.console.log "onLoadCompelte"
            state <- MainTitle

          let errorCallback e = Browser.console.error e
          let onProgress e = Browser.console.log e

          let addAssetToLoad (rawName: string) =
            let ressourceName =
              let fileName = rawName.Substring(rawName.LastIndexOf('/') + 1)
              fileName.Split('.').[0]
            Globals.loader.add(ressourceName, "assets/" + rawName)
            |> ignore


          [ "player/player_02"
          ]
          |> Seq.map(sprintf "%s.png")
          |> Seq.iter(addAssetToLoad)

          Globals.loader?on("error", errorCallback) |> ignore
          Globals.loader.load() |> ignore
          Globals.loader?on("progress", onProgress) |> ignore
          Globals.loader?once("complete", fun loader resources -> onLoadComplete resources) |> ignore
          Nothing
        | MainTitle ->
            Browser.console.log "MainTitle reached"
            Nothing

    let rec updateLoop render (dt: float) =
      promise {
        let mutable xs = []
        for x in sprites do
          do! x.Update(dt)
          if not x.IsDisposed then xs <- x::xs
        return xs
      }
      |> Promise.iter(fun sprites ->
        state <- update(state)
        render()
        Browser.window.requestAnimationFrame(
          fun dt ->
            updateLoop render dt
          ) |> ignore
        )

    updateLoop



  let start divName =
    let options =
      [ BackgroundColor (float 0x1099bb)
        Resolution 1.
        Antialias true
      ]

    let renderer =
      WebGLRenderer(Browser.window.innerWidth, Browser.window.innerHeight, options)

    Browser.document
      .getElementById(divName)
      .appendChild(renderer.view) |> ignore

    let stage = Container(interactive = true)
    updateLoop renderer stage (fun () -> renderer.render(stage)) 0.

  start "game"
