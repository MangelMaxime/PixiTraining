namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open Fable.Import.Matter

open PixiTraining.Inputs

open System
open System.Collections.Generic

module Main =

  let GRID = 32.
  //http://vasir.net/blog/game-development/how-to-build-entity-component-system-in-javascript

  type IComponent =
    interface end

  type Position =
    { mutable cx: int
      mutable cy: int
      mutable xr: float
      mutable yr: float
      mutable xx: float
      mutable yy: float
    }

    interface IComponent

    static member Create() =
      { cx = 0
        cy = 0
        xr = 0.
        yr = 0.
        xx = 0.
        yy = 0.
      }

  type Moveable =
    { mutable dx: float
      mutable Dy: float
    }

    interface IComponent

    static member Create(?dx, ?dy) =
      { dx = defaultArg dx 0.
        Dy = defaultArg dy 0.
      }

  type DisplayableGraphics =
    { mutable Sprite: Graphics
    }

    interface IComponent

  type UserControlled =
    inherit IComponent

  type Entity =
    { Id: String
      Components: Dictionary<string, IComponent>
    }

    member self.HasComponent<'T>() =
      self.Components.ContainsKey(typeof<'T>.Name)

    [<PassGenerics>]
    member self.GetComponent<'T>() =
      unbox<'T> self.Components.[typeof<'T>.Name]

  let addComponent<'T> comp entity =
    entity.Components.Add(typeof<'T>.Name, comp)
    entity

  let removeComponent key entity =
    entity.Components.Remove(key) |> ignore
    entity

  let createPlayer () =
    { Id = "player"
      Components = Dictionary<string, IComponent>()
    }
    |> addComponent<Moveable> (Moveable.Create())
    |> addComponent<Position> (Position.Create())
    |> addComponent<UserControlled> (Unchecked.defaultof<UserControlled>)

  let hasCollision cx cy (level: bool [] []) =
    if cx < 0 || cx > level.Length - 1 || cy >= level.[cx].Length then
      true
    else
      level.[cx].[cy]

  let generateLevel () =
    let rand = Random()
    let blocks = Graphics()
    let mutable level = [||]
    blocks.beginFill(float 0x525252) |> ignore

    for x = 0 to 31 do
      level.[x] <- [||]
      for y = 0 to 24 do
        level.[x].[y] <- y >= 22 || y > 3 && rand.Next(100) < 30
        if level.[x].[y] then
          blocks.drawRect(float x * GRID, float y * GRID, GRID, GRID) |> ignore

    blocks.endFill() |> ignore
    blocks

  type SceneState =
    { MouseState: Mouse.MouseState
      KeyboardState: Keyboard.KeyboardState
      Level: bool [][]
    }


  let options =
    [ BackgroundColor (float 0x9999bb)
      Resolution 1.
      Antialias true
    ]
  // Init the renderer
  let renderer = WebGLRenderer(1024., 800., options)
  // Init the canvas
  renderer.view.setAttribute("tabindex", "1")
  renderer.view.id <- "game"

  renderer.view.addEventListener_click(fun ev ->
    renderer.view.focus()
    null
  )

  Browser.document.body
    .appendChild(renderer.view) |> ignore

  renderer.view.focus()

  let root = Container()

  let mouseState = Mouse.init root
  let keyboardState = Keyboard.init renderer.view

  let systemUserInputs (entities: Entity list) =
    for entity in entities do
      if entity.HasComponent<UserControlled>() && entity.HasComponent<Moveable>() then
        let moveable = entity.GetComponent<Moveable>()
        let speed = 0.04

        if keyboardState.IsPress(Keyboard.Keys.ArrowRight) then
          moveable.dx <- moveable.dx + speed
        else if keyboardState.IsPress(Keyboard.Keys.ArrowLeft) then
          moveable.dx <- moveable.dx - speed


  let systemPhysics (sceneState: SceneState) (entities: Entity list) =
    for entity in entities do
      if entity.HasComponent<Position>() && entity.HasComponent<Moveable>() then
        let position = entity.GetComponent<Position>()
        let moveable = entity.GetComponent<Moveable>()

        let frictX = 0.75
        let frictY = 0.94
        let gravity = 0.04

        // X Components
        position.xr <- position.xr + moveable.dx
        moveable.dx <- moveable.dx + frictX

        if hasCollision (position.cx - 1) position.cy sceneState.Level && position.xr <= 0.3 then
          moveable.dx <- 0.
          position.xr <- 0.3

        if hasCollision (position.cx + 1) position.cy sceneState.Level && position.xr >= 0.7 then
          moveable.dx <- 0.
          position.xr <- 0.7

        while position.xr < 0. do
          position.cx <- position.cx - 1
          position.xr <- position.xr + 1.

        while position.xr > 1. do
          position.cx <- position.cx + 1
          position.xr <- position.xr - 1.

        position.xx <- (float position.cx + position.xr) * GRID
        position.yy <- (float position.cy + position.yr) * GRID

  let systemRenderer (entities: Entity list) =
    for entity in entities do
      if entity.HasComponent<Position>() && entity.HasComponent<DisplayableGraphics>() then
        let position = entity.GetComponent<Position>()
        let display = entity.GetComponent<DisplayableGraphics>()

        display.Sprite.x <- position.xx
        display.Sprite.y <- position.yy

  type Scene (engine) as self =


    member val Root : Container = Container() with get
    member val MouseState = Unchecked.defaultof<Mouse.MouseState> with get, set
    member val KeyboardState = Unchecked.defaultof<Keyboard.KeyboardState> with get, set
    member val Level : bool [] [] = [||]
    member val Blocks : Graphics = Unchecked.defaultof<Graphics> with get, set
    member val Engine : Engine = engine with get, set
    member val Player = Entity(self).Init() with get, set
    member val InfoText: PIXI.Text = Unchecked.defaultof<PIXI.Text> with get, set

    member self.Init() =
      self.MouseState <- Mouse.init self.Root
      self.KeyboardState <- Keyboard.init engine.Canvas

      self.InfoText <- PIXI.Text("Use arrows to move. \nPress R to start a new level")
      self.Root.addChild(self.InfoText) |> ignore

      self.GenerateLevel()
      self

    member self.GenerateLevel() =
      self.Blocks <- Graphics()
      self.Root.addChild(self.Blocks) |> ignore
      self.Blocks.beginFill(float 0x525252) |> ignore

      for x = 0 to 31 do
        self.Level.[x] <- [||]
        for y = 0 to 24 do
          self.Level.[x].[y] <- y >= 22 || y > 3 && rand.Next(100) < 30
          if self.Level.[x].[y] then
            self.Blocks.drawRect(float x * GRID, float y * GRID, GRID, GRID) |> ignore

      self.Blocks.endFill() |> ignore

    member self.Update(dt: float) =
      let speed = 0.04

      if self.KeyboardState.IsPress(Keyboard.Keys.ArrowRight) then
        self.Player.dx <- self.Player.dx + speed

      if self.KeyboardState.IsPress(Keyboard.Keys.ArrowLeft) then
        self.Player.dx <- self.Player.dx - speed

      if self.KeyboardState.IsPress(Keyboard.Keys.ArrowUp) && self.Player.OnGround() then
        self.Player.dy <- 0.7

      if self.KeyboardState.IsPress(Keyboard.Keys.R) then
        self.Root.removeChild(self.Blocks) |> ignore
        self.GenerateLevel()
        self.Player.SetCoordinates(5 * int GRID, 0)

      self.Player.Update(dt)
      ()

  type Engine () =
    member val Renderer = Unchecked.defaultof<WebGLRenderer> with get, set
    member val Canvas = Unchecked.defaultof<Browser.HTMLCanvasElement> with get, set
    member val StartDate : DateTime = DateTime.Now with get, set
    member val LastTickDate = 0. with get, set
    member val DeltaTime = 0. with get, set
    member val Scene: Scene option = None with get, set

    member self.Init () =
      let options =
        [ BackgroundColor (float 0x9999bb)
          Resolution 1.
          Antialias true
        ]
      // Init the renderer
      self.Renderer <- WebGLRenderer(1024., 800., options)
      // Init the canvas
      self.Canvas <- self.Renderer.view
      self.Canvas.setAttribute("tabindex", "1")
      self.Canvas.id <- "game"

      self.Canvas.addEventListener_click(fun ev ->
        self.Canvas.focus()
        null
      )

      Browser.document.body
        .appendChild(self.Canvas) |> ignore

      self.Canvas.focus()

    member self.Start() =
      self.StartDate <- DateTime.Now
      self.RequestUpdate()

    member self.RequestUpdate() =
      Browser.window.requestAnimationFrame(fun dt -> self.Update(dt)) |> ignore

    member self.Update(dt: float) =
      match self.Scene with
      | Some scene ->
          scene.Update(dt)
          self.Renderer.render(scene.Root)
      | None -> Browser.console.warn "No scene."
      self.RequestUpdate()

    member self.SetScene(scene) =
      self.Scene <- Some scene


  // Create and init the engine instance
  let engine = new Engine()
  engine.Init()


  let scene = Scene(engine).Init()
  engine.SetScene(scene)
  engine.Start()
