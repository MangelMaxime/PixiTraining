namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open Fable.PowerPack

open PixiTraining.Inputs

open System

module Main =

  type BasicSceneDU
    = Init
    | Run
    | Pause

  type Scene
    = Level1 of BasicSceneDU

  type LoadingDU
    = Init
    | Waiting

  type State
    = Nothing
    | Loading of LoadingDU
    | Scene of Scene
    | SplashScreen of BasicSceneDU

  type GameState =
    { Renderer: WebGLRenderer
      Id: int
      SpriteSheets: Map<string, ResizeArray<Texture>>
      State: State
      Root: Container
      MouseState: Inputs.Mouse.MouseState
      KeyboardState: Inputs.Keyboard.KeyboardState
      Data: obj
      Resources: loaders.ResourceDictionary
      DeltaTime: float
      PreviousTime: float
      Bounds: Rectangle
    }

    [<PassGenerics>]
    member self.GetData<'T>() =
      unbox<'T>  self.Data

    member self.ClearStage() =
      self.Root.removeChildren() |> ignore
      self

    member self.Render() =
      self.Renderer.render(self.Root)
      self.KeyboardState.ClearLastKey()

    member self.GetCenterX () =
      self.Renderer.width / 2.

    member self.GetCenterY () =
      self.Renderer.height / 2.

    member self.PlayerBlue
      with get () = self.SpriteSheets.[PlayerBlue.SPRITES_SHEETS_KEY]

  type SplashScreenState =
    { StartTime: DateTime
    }

  let CONTAINER_GROUND = "ground"

  type PlayerState =
    { Sprite: Sprite
      Health: int
      Speed: float
    }

    member self.X
      with get () = self.Sprite.x
      and set (value) = self.Sprite.x <- value

    member self.Y
      with get () = self.Sprite.y
      and set (value) = self.Sprite.y <- value

    static member Create (sprite) =
      { Sprite = sprite
        Health = 3
        Speed = 0.5
      }

  type StateLevel1 =
    { EntitiesContainer: Container
      Player: PlayerState
    }

  let kickGame (initialState: GameState) =
    let mutable state = initialState

    let rec gameLoop (gameState: GameState) (dt: float) =

      let gameState =
        { gameState with
            PreviousTime = dt
            DeltaTime = if dt = 0. then 0. else dt - gameState.PreviousTime
        }

      let newState =
        match gameState.State with
        | Nothing -> gameState
        | Loading subState ->
            match subState with
            | LoadingDU.Init ->
                let state' = gameState.ClearStage()

                let onError (e) = Browser.console.error e
                let onProgress (e) = Browser.console.log e?progress

                let onLoadComplete r =
                  let resources = unbox<loaders.ResourceDictionary> r

                  state <-
                    { state with
                        Resources = resources
                        State = Scene (Level1 BasicSceneDU.Init)
                        SpriteSheets =
                          state.SpriteSheets
                            .Add(PlayerBlue.SPRITES_SHEETS_KEY, resources.Item("player_blue").textures)
                    }

                let addAssetToLoad (rawName: string) =
                  let ressourceName =
                    let fileName = rawName.Substring(rawName.LastIndexOf('/') + 1)
                    fileName.Split('.').[0]
                  Globals.loader.add(ressourceName, "assets/" + rawName)
                  |> ignore

                [ "player_blue.json"
                ]
                |> Seq.iter(addAssetToLoad)

                Globals.loader?on("error", onError) |> ignore
                Globals.loader.load() |> ignore
                Globals.loader?on("progress", onProgress) |> ignore
                Globals.loader?once("complete", fun _ resources -> onLoadComplete resources) |> ignore

                { state' with State = Loading Waiting }

            | Waiting -> gameState
        | Scene subScene ->
            match subScene with
            | Level1 sceneDU ->
                match sceneDU with
                | BasicSceneDU.Init ->
                    let state' = gameState.ClearStage()

                    let entitiesContainer = new Container()

                    let player =
                      state.PlayerBlue?(PlayerBlue.SPRITE_WALK_1)
                      |> unbox<Texture>
                      |> makeSprite

                    createText("Level 1")
                    |> setAnchor 0.5 0.5
                    |> setPosition (state'.GetCenterX()) 50.
                    |> (fun x -> state'.Root.addChild(x))
                    |> ignore

                    let data =
                      { EntitiesContainer = entitiesContainer
                        Player = PlayerState.Create(player)
                      }

                    entitiesContainer.addChild(player) |> ignore

                    [ data.EntitiesContainer
                    ]
                    |> (fun x -> state'.Root.addChild(unbox x))
                    |> ignore

                    { state' with
                        State = Scene (Level1 Run)
                        Data = data }
                | Run ->
                    // Add here the game logic
                    // Example: Player movement
                    let data = gameState.GetData<StateLevel1>()

                    let playerWalk (gameState: GameState) (data: StateLevel1) =
                      let keyboard = gameState.KeyboardState

                      let dir =
                        if keyboard.IsPress(Keyboard.Keys.ArrowRight) then
                          1.
                        else if keyboard.IsPress(Keyboard.Keys.ArrowLeft) then
                          -1.
                        else
                          0.

                      data.Player.X <- data.Player.X + (dir * data.Player.Speed * gameState.DeltaTime)
                      data

                    let stepsPlayer data =
                      data
                      |> playerWalk gameState

                    let stepScene data =
                      data
                      |> stepsPlayer

                    { gameState with
                        Data = stepScene data }
                | Pause -> gameState

        | SplashScreen sceneDU ->
            let data = gameState.GetData<SplashScreenState>()

            match sceneDU with
            | BasicSceneDU.Init ->
                let state' = gameState.ClearStage()
                createText("Fable graphics")
                |> setAnchor 0.5 0.5
                |> setPosition (state'.GetCenterX()) (state'.GetCenterY())
                |> (fun x -> state'.Root.addChild(x))
                |> ignore
                { state' with State = SplashScreen Run }
            | Run ->
                let elapsedTime = DateTime.Now - data.StartTime
                if elapsedTime > TimeSpan.FromSeconds(1.) then
                  { gameState with State = Loading LoadingDU.Init }
                else
                  gameState
            | Pause -> gameState

      state <- newState
      state.Render()

      Browser.window.requestAnimationFrame(fun dt ->
        gameLoop state dt)
      |> ignore

    gameLoop state 0.

  let launchGame divName =

    let width = 1024.
    let height = 800.

    let options =
      [ BackgroundColor (float 0x9999bb)
        Resolution 1.
        Antialias true
      ]

    let renderer =
      WebGLRenderer(width, height, options)

    Browser.document
      .getElementById(divName)
      .appendChild(renderer.view) |> ignore

    // Make the canvas selectable
    renderer.view.setAttribute("tabindex", "1")
    renderer.view.focus()

    // Attach a top click event to game focus on click
    renderer.view.addEventListener_click(fun ev ->
      renderer.view.focus()
      null
    )

    let stage = Container(interactive = true)
    stage.hitArea <- Rectangle(0., 0., width, height)

    let state =
      { Renderer = renderer
        Id = -1
        SpriteSheets = Map.empty
        State = SplashScreen BasicSceneDU.Init
        Root = stage
        MouseState = Inputs.Mouse.init stage
        KeyboardState = Inputs.Keyboard.init renderer.view
        Data =
          { StartTime = DateTime.Now
          }
        DeltaTime = 0.
        PreviousTime = 0.
        Resources = Unchecked.defaultof<loaders.ResourceDictionary>
        Bounds = Rectangle(0., 0., width, height)
      }

    kickGame state

  launchGame "game"
