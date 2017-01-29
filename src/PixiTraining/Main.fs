namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open Fable.PowerPack

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

    member self.Survivors
      with get () = self.SpriteSheets.[Survivors.SPRITES_SHEETS_KEY]

    member self.Tiles
      with get () = self.SpriteSheets.[Tiles.SPRITES_SHEETS_KEY]

  type SplashScreenState =
    { StartTime: DateTime
    }

  let CONTAINER_GROUND = "ground"

  type PlayerState =
    { Sprite: ESprite
      FireDelay: TimeSpan
      LastShootTime: DateTime
    }

    member self.X
      with get () = self.Sprite.x
      and set (value) = self.Sprite.x <- value

    member self.Y
      with get () = self.Sprite.y
      and set (value) = self.Sprite.y <- value

    static member Create (sprite, ?delay) =
      { Sprite = sprite
        FireDelay = TimeSpan.FromMilliseconds(defaultArg delay 150.)
        LastShootTime = DateTime.Now
      }

  type Bullets =
    { Direction: Vector
      Damage: float
      Sprite: Sprite
    }

    member self.__X
      with get () = self.Sprite.x
      and set (value) = self.Sprite.x <- value

    member self.__Y
      with get () = self.Sprite.y
      and set (value) = self.Sprite.y <- value

  type StateLevel1 =
    { BulletsContainer: Container
      EntitiesContainer: Container
      Bullets: Bullets list
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
                            .Add(Tiles.SPRITES_SHEETS_KEY, resources.Item("tiles_sheets").textures)
                            .Add(Survivors.SPRITES_SHEETS_KEY, resources.Item("survivors_sheets").textures)
                    }

                let addAssetToLoad (rawName: string) =
                  let ressourceName =
                    let fileName = rawName.Substring(rawName.LastIndexOf('/') + 1)
                    fileName.Split('.').[0]
                  Globals.loader.add(ressourceName, "assets/" + rawName)
                  |> ignore

                [ "tiles_sheets.json"
                  "survivors_sheets.json"
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
                      state.Survivors?(Survivors.SPRITE_GUN)
                      |> unbox<Texture>
                      |> makeESprite [] "player"
                      |> setPosition 100. 100.
                      |> addToContainer entitiesContainer

                    createText("Level 1")
                    |> setPosition (state'.GetCenterX()) 50.
                    |> setAnchor 0.5 0.5
                    |> (fun x -> state'.Root.addChild(x))
                    |> ignore

                    let data =
                      { BulletsContainer = new Container()
                        EntitiesContainer = entitiesContainer
                        Bullets = []
                        Player = PlayerState.Create(player :?> ESprite)
                      }

                    [ data.BulletsContainer
                      data.EntitiesContainer
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

                    let moveBullets (data: StateLevel1) =
                      for bullet in data.Bullets do
                        bullet.__X <- bullet.__X + 0.8 * gameState.DeltaTime
                      data

                    let isSpriteOffScreen (bounds: Rectangle) (sprite: Sprite) =
                      let sx = sprite.position.x
                      let sy = sprite.position.y

                      (sx + sprite.width) < bounds.x
                        || (sy + sprite.height) < bounds.y
                        || (sprite.y - sprite.height) >= bounds.height
                        || (sx - sprite.width) > bounds.width

                    let killBulletsOffScreen (bounds: Rectangle) (data: StateLevel1) =
                      let liveBullets =
                        data.Bullets
                        |> List.filter(fun x ->
                          not (isSpriteOffScreen bounds x.Sprite)
                        )
                      { data with Bullets = liveBullets }

                    let createBullet originX originY mousePosition =

                      gameState.Tiles?(Tiles.SPRITE_GUN_BULLET)
                      |> unbox<Texture>
                      |> makeSprite
                      |> setPosition originX originY
                      |> addToContainer data.BulletsContainer
                      |> fun x ->
                          { Direction = Vector(1., 1.)
                            Damage = 1.
                            Sprite = x
                          }

                    let playerInputsBullet (inputs: Inputs.Mouse.MouseState) (data: StateLevel1) =
                      if gameState.MouseState.Left && (now - data.Player.LastShootTime) > data.Player.FireDelay then
                        let originX = data.Player.X + 50.
                        let originY = data.Player.Y + 29.
                        let bullet = createBullet originX originY gameState.MouseState.Position
                        { data with
                            Bullets = bullet :: data.Bullets
                            Player =
                              { data.Player with
                                  LastShootTime = now
                              }
                        }
                      else
                        data

                    let stepBullets data =
                      data
                      |> moveBullets
                      |> killBulletsOffScreen gameState.Bounds
                      |> playerInputsBullet gameState.MouseState

                    { gameState with
                        Data = stepBullets data }
                | Pause -> gameState

        | SplashScreen sceneDU ->
            let data = gameState.GetData<SplashScreenState>()

            match sceneDU with
            | BasicSceneDU.Init ->
                let state' = gameState.ClearStage()
                createText("Fable graphics")
                |> setPosition (state'.GetCenterX()) (state'.GetCenterY())
                |> setAnchor 0.5 0.5
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
