namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open Fable.PowerPack

open System

module Main =

  type Scene
    = Level1


  type Actions
    = Nothing
    | Loading
    | RunScene of Scene
    | RessourcesLoaded of loaders.ResourceDictionary

  module Behaviors =
    let moveable = ()

  open Behaviors

  type GameState =
    { Fps: float
      Id: int
      Resources: loaders.ResourceDictionary
      SpriteSheets: Map<string, ResizeArray<Texture>>
      Sprites: ESprite list
      Renderer: WebGLRenderer
      Stage: Container
      MouseState: Inputs.MouseState
    }

    static member Initial (renderer: WebGLRenderer) =
      let stage = new Container()

      { Fps = 60.
        Id = -1
        Resources = Unchecked.defaultof<loaders.ResourceDictionary>
        SpriteSheets = Map.empty
        Sprites = []
        Renderer = renderer
        Stage = stage
        MouseState = Inputs.initMouse stage
      }

  let initialGameState = GameState.Initial

  let updateLoop2 (gameState: GameState) action =
      match action with
      | Nothing ->
          gameState, []
      | RessourcesLoaded resources ->
          let spriteSheets =
            gameState.SpriteSheets
              .Add("sprites", resources.Item("sprites").textures)
              .Add("survivors_sheets", resources.Item("survivors_sheets").textures)
          { gameState with
              SpriteSheets = spriteSheets }, []
      | Run ->
          let message =
            [ fun h ->
                let onLoadComplete r =
                  let resources = unbox<loaders.ResourceDictionary> r

                  Browser.console.log "onLoadCompelte"


                let errorCallback e = Browser.console.error e
                let onProgress e = Browser.console.log e?progress

                let addAssetToLoad (rawName: string) =
                  let ressourceName =
                    let fileName = rawName.Substring(rawName.LastIndexOf('/') + 1)
                    fileName.Split('.').[0]
                  Globals.loader.add(ressourceName, "assets/" + rawName)
                  |> ignore

                [ "sprites.json"
                  "survivors_sheets.json"
                ]
                |> Seq.iter(addAssetToLoad)

                Globals.loader?on("error", errorCallback) |> ignore
                Globals.loader.load() |> ignore
                Globals.loader?on("progress", onProgress) |> ignore
                Globals.loader?once("complete", fun _ resources -> onLoadComplete resources) |> ignore
            ]

          gameState, []


  type State =
    | Nothing
    | Loading
    | Play

  let updateLoop (renderer: WebGLRenderer) (stage:Container) =

    let fps = 60.
    let mutable state : State = Loading
    let mutable id = -1
    let mutable resources = Unchecked.defaultof<loaders.ResourceDictionary>
    let mutable resTiles = Unchecked.defaultof<ResizeArray<Texture>>
    let mutable survivorSheet = Unchecked.defaultof<ResizeArray<Texture>>
    let mutable sprites : ESprite list = []

    let testContainer = Container()

    let bindContainer (c:DisplayObject) = stage.addChild c |> ignore
    let stageMouseState = Inputs.initMouse stage

    [ testContainer ]
    |> Seq.iter(bindContainer)

    let nextId () =
      id <- id + 1
      sprintf "%i" id

    let update (currentState: State) =

      match currentState with
      | Nothing -> State.Nothing
      | Loading ->
          let onLoadComplete r =
            resources <- unbox<loaders.ResourceDictionary> r

            resTiles <- resources.Item("sprites").textures
            survivorSheet <- resources.Item("survivors_sheets").textures

            Browser.console.log "onLoadCompelte"
            state <- Play

          let errorCallback e = Browser.console.error e
          let onProgress e = Browser.console.log e?progress

          let addAssetToLoad (rawName: string) =
            let ressourceName =
              let fileName = rawName.Substring(rawName.LastIndexOf('/') + 1)
              fileName.Split('.').[0]
            Globals.loader.add(ressourceName, "assets/" + rawName)
            |> ignore

          [ "sprites.json"
            "survivors_sheets.json"
          ]
          |> Seq.iter(addAssetToLoad)

          Globals.loader?on("error", errorCallback) |> ignore
          Globals.loader.load() |> ignore
          Globals.loader?on("progress", onProgress) |> ignore
          Globals.loader?once("complete", fun _ resources -> onLoadComplete resources) |> ignore
          Nothing
        | Play ->
            resTiles?("dirt_1.png")
            |> unbox<Texture>
            |> makeSprite
            |> setPosition 100. 100.
            |> addToContainer testContainer
            |> ignore

            resTiles?("grass_1.png")
            |> unbox<Texture>
            |> makeSprite
            |> addToContainer testContainer
            |> ignore

            survivorSheet?("survivor1_gun.png")
            |> unbox<Texture>
            |> makeESprite [] (nextId())
            |> centerPivot
            |> setPosition 200. 200.
            |> setRotation (degreesToRad 90.)
            |> addToContainer testContainer
            |> ignore

            survivorSheet?("survivor1_gun.png")
            |> unbox<Texture>
            |> makeESprite [] (nextId())
            |> centerPivot
            |> setPosition 200. 200.
            |> addToContainer testContainer
            |> ignore

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
    stage.hitArea <- Rectangle(0., 0., Browser.window.innerWidth, Browser.window.innerHeight)

    updateLoop renderer stage (fun () -> renderer.render(stage)) 0.

  start "game"
