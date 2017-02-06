namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI

open PixiTraining.Inputs

open System
open System.Collections.Generic

module Main =

  let GRID = 32.
  //http://vasir.net/blog/game-development/how-to-build-entity-component-system-in-javascript

  type Scene (engine) =
    member val Root : Container = Container() with get
    member val Engine : Engine = engine with get, set
    member val Entities : Entity list = [] with get, set

    member self.Init() =
      self

    member self.Update (_: float) = ()

  and Engine () =
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

      // Init inputs managers
      Inputs.Mouse.init self.Canvas
      Inputs.Keyboard.init self.Canvas true

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

  and IComponent =
    interface end

  and Entity =
    { Id: String
      Components: Dictionary<string, IComponent>
    }

    [<PassGenerics>]
    member self.HasComponent<'T>() =
      self.Components.ContainsKey(typeof<'T>.Name)

    [<PassGenerics>]
    member self.GetComponent<'T>() =
      unbox<'T> self.Components.[typeof<'T>.Name]

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
      mutable dy: float
    }

    interface IComponent

    static member Create(?dx, ?dy) =
      { dx = defaultArg dx 0.
        dy = defaultArg dy 0.
      }

  type DisplayableGraphics =
    { mutable Sprite: Graphics
    }

    interface IComponent

    static member Create(sprite) =
      { Sprite = sprite
      }

  type UserControlled =
    inherit IComponent

  [<PassGenerics>]
  let addComponent<'T> comp entity =
    entity.Components.Add(typeof<'T>.Name, comp)
    entity

  let removeComponent key entity =
    entity.Components.Remove(key) |> ignore
    entity

  let createPlayer (root: Container) =
    let sprite = Graphics()
    sprite.beginFill(float 0xFFFF00) |> ignore
    sprite.drawCircle(0., 0., GRID * 0.5) |> ignore
    sprite.endFill() |> ignore
    root.addChild(sprite) |> ignore

    { Id = "player"
      Components = Dictionary<string, IComponent>()
    }
    |> addComponent<Moveable> (Moveable.Create())
    |> addComponent<Position> (Position.Create())
    |> addComponent<UserControlled> (Unchecked.defaultof<UserControlled>)
    |> addComponent<DisplayableGraphics> (DisplayableGraphics.Create(sprite))

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

  let systemUserInputs (entities: Entity list) =
    for entity in entities do
      if entity.HasComponent<UserControlled>() && entity.HasComponent<Moveable>() then
        let moveable = entity.GetComponent<Moveable>()
        let speed = 0.04
        ()
        if Keyboard.Manager.IsPress(Keyboard.Keys.ArrowRight) then
          moveable.dx <- moveable.dx + speed
        else if Keyboard.Manager.IsPress(Keyboard.Keys.ArrowLeft) then
          moveable.dx <- moveable.dx - speed
    entities

  let systemPhysics (entities: Entity list) =
    for entity in entities do
      if entity.HasComponent<Position>() && entity.HasComponent<Moveable>() then
        let position = entity.GetComponent<Position>()
        let moveable = entity.GetComponent<Moveable>()

        let frictX = 0.75
        let frictY = 0.94
        let gravity = 0.04

        // X Components
        position.xr <- position.xr + moveable.dx
        moveable.dx <- moveable.dx * frictX

//        if hasCollision (position.cx - 1) position.cy level && position.xr <= 0.3 then
//          moveable.dx <- 0.
//          position.xr <- 0.3
//
//        if hasCollision (position.cx + 1) position.cy level && position.xr >= 0.7 then
//          moveable.dx <- 0.
//          position.xr <- 0.7

        while position.xr < 0. do
          position.cx <- position.cx - 1
          position.xr <- position.xr + 1.

        while position.xr > 1. do
          position.cx <- position.cx + 1
          position.xr <- position.xr - 1.

        // Y components
        moveable.dy <- moveable.dy - gravity
        position.yr <- position.yr - moveable.dy
        moveable.dy <- moveable.dy * frictY

        while position.yr < 0. do
          position.cy <- position.cy - 1
          position.yr <- position.yr + 1.

        while position.yr > 1. do
          position.cy <- position.cy + 1
          position.yr <- position.yr - 1.

        position.xx <- (float position.cx + position.xr) * GRID
        position.yy <- (float position.cy + position.yr) * GRID

    entities

  let systemRenderer (entities: Entity list) =
    for entity in entities do
      if entity.HasComponent<Position>() && entity.HasComponent<DisplayableGraphics>() then
        let position = entity.GetComponent<Position>()
        let display = entity.GetComponent<DisplayableGraphics>()

        display.Sprite.x <- position.xx
        display.Sprite.y <- position.yy
    entities


  type SceneLevel1 (engine) =
    inherit Scene(engine)

    member self.Init() =
      self.Entities <-
        [ createPlayer self.Root
        ]

      base.Init()

    member self.Update (dt: float) =
      self.Entities
      |> systemUserInputs
      |> systemPhysics
      |> systemRenderer
      |> ignore
      ()


  // Create and init the engine instance
  let engine = new Engine()
  engine.Init()

  let scene = SceneLevel1(engine).Init()
  engine.SetScene(scene)
  engine.Start()
