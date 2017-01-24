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


  type TileDef =
    { Name: string
      X: float
      Y: float
      Width: float
      Height: float
    }

    member self.ToRectangle () =
      Rectangle(self.X, self.Y, self.Width, self.Height)

  type SpriteSheet =
    { Name: string
      Tiles: Map<string, Sprite>
    }

  let createSpriteSheet (res: loaders.ResourceDictionary) name (tilesDef: TileDef list) =
    let rec createTiles (res: loaders.ResourceDictionary) (tilesDef: TileDef list) (tiles: Map<string, Sprite>) =
      match tilesDef with
      | x::xs ->
          let texture = (res.Item(name).texture)
          texture.frame <- x.ToRectangle()
          createTiles res xs (tiles.Add(x.Name, Sprite(texture)))
      | [] -> tiles

    { Name = name
      Tiles = createTiles res tilesDef Map.empty
    }


  let Grass1 =
    { Name = "Grass1"
      X = 0.
      Y = 0.
      Width = 64.
      Height = 64.
    }

  let Grass2 =
    { Name = "Grass2"
      X = 74.
      Y = 0.
      Width = 64.
      Height = 64.
    }

  module Behaviors =
    let moveable = ()

  open Behaviors

  let updateLoop (renderer: WebGLRenderer) (stage:Container) =

    let fps = 60.
    let mutable state : State = Loading
    let mutable id = -1
    let mutable resources  = Unchecked.defaultof<loaders.ResourceDictionary>
    let mutable sprites = []

    let mutable tilesSheet = Unchecked.defaultof<SpriteSheet>

    let testContainer = Container()

    let bindContainer (c:DisplayObject) = stage.addChild c |> ignore

    [ testContainer ]
    |> Seq.iter(bindContainer)

    let nextId () =
      id <- id + 1
      sprintf "%i" id
    let makeSprite t =
      Sprite t
    let makeESprite (behaviors: Behavior list) (t: Texture) =
      new ESprite(t, nextId(), behaviors)
    let getTexture name =
      resources.Item(name).texture
    let addToContainer (c: Container) (s: Sprite) =
      c.addChild s |> ignore
      s
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
            resources <- unbox<loaders.ResourceDictionary>r
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

          [ "sprites.json"
          ]
          |> Seq.iter(addAssetToLoad)

          Globals.loader?on("error", errorCallback) |> ignore
          Globals.loader.load() |> ignore
          Globals.loader?on("progress", onProgress) |> ignore
          Globals.loader?once("complete", fun loader resources -> onLoadComplete resources) |> ignore
          Nothing
        | MainTitle ->

//            tilesSheet.Tiles.Item("Grass1")
//            |> addToContainer testContainer
//            |> ignore
//
//            tilesSheet.Tiles.Item("Grass2")
//            |> setPosition 100. 100.
//            |> addToContainer testContainer
//            |> ignore

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
      [ BackgroundColor (float 0x9999bb)
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
