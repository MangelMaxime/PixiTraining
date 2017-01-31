namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open Fable.Import.Matter

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

  type Direction
    = Right
    | Left

  type PlayerAnim
    = Stand
    | Walk of Direction

  type PlayerState =
    { Sprite: Sprite
      Health: int
      Speed: float
      ActiveFrame: int
      Frames: Texture list
      FrameStart: DateTime
      AnimType: PlayerAnim
      Body: Matter.Body
    }

    member self.X
      with get () = self.Sprite.x
      and set (value) = self.Sprite.x <- value

    member self.Y
      with get () = self.Sprite.y
      and set (value) = self.Sprite.y <- value

    member self.SetFrame (index: int) =
      self.Sprite.texture <- self.Frames.[index]

    member self.NextFrame (now: DateTime) =
      let nextFrameId = self.ActiveFrame + 1
      if nextFrameId > self.Frames.Length - 1 then
        self.Sprite.texture <- self.Frames.[0]
        { self with
            ActiveFrame = 0
            FrameStart = now
        }
      else
        self.Sprite.texture <- self.Frames.[nextFrameId]
        { self with
            ActiveFrame = nextFrameId
            FrameStart = now
        }

    static member Create (sprite, frames, body) =
      { Sprite = sprite
        Health = 3
        Speed = 0.5
        ActiveFrame = 0
        Frames = frames
        FrameStart = DateTime.Now
        AnimType = Stand
        Body = body
      }

  type StateLevel1 =
    { EntitiesContainer: Container
      Player: PlayerState
      Ground: Matter.Body
      World: Matter.World
      Engine: Matter.Engine
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

                    let playerSprite, playerBody =
                      let sprite =
                        state.PlayerBlue?(PlayerBlue.SPRITE_WALK_1)
                        |> unbox<Texture>
                        |> makeSprite
                        |> setPosition 50. 150.

                      let body =
                        Matter.Bodies.rectangle(sprite.position.x, sprite.position.y, sprite.width, sprite.height)
                      sprite, body

                    createText("Level 1")
                    |> setAnchor 0.5 0.5
                    |> setPosition (state'.GetCenterX()) 50.
                    |> (fun x -> state'.Root.addChild(x))
                    |> ignore

                    let data =
                      let engine = Matter.Engine.create(options=null)
                      let groundOptions =
                        [ IsStatic
                        ]
                      { EntitiesContainer = entitiesContainer
                        Player =
                          PlayerState.Create(
                            playerSprite,
                            [ !!state.PlayerBlue?(PlayerBlue.SPRITE_WALK_1)
                              !!state.PlayerBlue?(PlayerBlue.SPRITE_WALK_2)
                              !!state.PlayerBlue?(PlayerBlue.SPRITE_WALK_3)
                              !!state.PlayerBlue?(PlayerBlue.SPRITE_WALK_4)
                              !!state.PlayerBlue?(PlayerBlue.SPRITE_WALK_5)
                            ],
                            playerBody
                          )
                        World = engine.world
                        Engine = engine
                        Ground = Matter.Bodies.rectangle(0., 600., gameState.Bounds.width, 60., !!groundOptions)
                      }

                    Matter.World.add(data.World, [data.Player.Body, data.Ground]) |> ignore

                    entitiesContainer.addChild(playerSprite) |> ignore

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
                    let now = DateTime.Now

                    !!Matter.Engine.update(data.Engine)

                    Browser.console.log data.Player.Body.position.y

                    let playerWalk (gameState: GameState) (playerState: PlayerState) =
                      let keyboard = gameState.KeyboardState

                      let dir, anim =
                        if keyboard.IsPress(Keyboard.Keys.ArrowRight) then
                          1., Walk Right
                        else if keyboard.IsPress(Keyboard.Keys.ArrowLeft) then
                          -1., Walk Left
                        else
                          0., Stand

                      playerState.X <- playerState.X + (dir * playerState.Speed * gameState.DeltaTime)

                      // Update sprite orientation
                      match anim with
                      | Walk Right -> playerState.Sprite.scale.x <- 1.
                      | Walk Left -> playerState.Sprite.scale.x <- -1.
                      | Stand -> playerState.SetFrame(0); ()

                      { playerState with
                          AnimType = anim }

                    let animPlayer (playerState: PlayerState) =
                      if now - playerState.FrameStart > TimeSpan.FromMilliseconds(100.) then
                        match playerState.AnimType with
                        | Stand -> playerState
                        | Walk _ -> playerState.NextFrame(now)
                      else
                        playerState

                    let stepsPlayer data =
                      data.Player
                      |> playerWalk gameState
                      |> animPlayer
                      |> fun x -> { data with Player = x }

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
