namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI

open PixiTraining.Inputs

open System

module Main =

  let GRID = 32.

  type Scene (engine) as self =
    member val GraphicRoot : Container = Container() with get
    member val Engine : Engine = engine with get, set

    member self.Init() =
      self

    member self.Update(dt: float) =
      ()

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

      Mouse.init self.Canvas
      Keyboard.init self.Canvas true

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
          self.Renderer.render(scene.GraphicRoot)
      | None -> Browser.console.warn "No scene."
      self.RequestUpdate()

    member self.SetScene(scene) =
      self.Scene <- Some scene

  type Entity (scene) =

    // Base coordinates
    member val cx = 5 with get, set
    member val cy = 0 with get, set
    member val xr = 0.5 with get, set
    member val yr = 0.5 with get, set
    // Resulting coordinates
    member val xx = 0. with get, set
    member val yy = 0. with get, set
    // Graphical object
    member val Graphics : Graphics = Graphics() with get, set
    // Scene instance
    member val scene: Level1 = scene with get, set
    // Movements
    member val dx = 0. with get, set
    member val dy = 0. with get, set

    member self.Init() =
      self.Graphics.beginFill(float 0xFFFF00) |> ignore
      self.Graphics.drawCircle(0., 0., GRID * 0.5) |> ignore
      self.Graphics.endFill() |> ignore

      self.scene.GraphicRoot.addChild(self.Graphics) |> ignore
      self

    member self.Update(dt: float) =
      let frictX = 0.75
      let frictY = 0.94
      let gravity = 0.04

      // X component
      self.xr <- self.xr + self.dx
      self.dx <- self.dx * frictX

      if self.HasCollision(self.cx - 1, self.cy) && self.xr <= 0.3 then
        self.dx <- 0.
        self.xr <- 0.3

      if self.HasCollision(self.cx + 1, self.cy) && self.xr >= 0.7 then
        self.dx <- 0.
        self.xr <- 0.7

      while self.xr < 0. do
        self.cx <- self.cx - 1
        self.xr <- self.xr + 1.

      while self.xr > 1. do
        self.cx <- self.cx + 1
        self.xr <- self.xr - 1.

      // Y component
      self.dy <- self.dy - gravity
      self.yr <- self.yr - self.dy
      self.dy <- self.dy * frictY

      if self.HasCollision(self.cx, self.cy - 1 ) && self.yr <= 0.4 then
        self.dy <- 0.
        self.yr <- 0.4

      if self.HasCollision(self.cx, self.cy + 1) && self.yr >= 0.5 then
        self.dy <- 0.
        self.yr <- 0.5

      while self.yr < 0. do
        self.cy <- self.cy - 1
        self.yr <- self.yr + 1.

      while self.yr > 1. do
        self.cy <- self.cy + 1
        self.yr <- self.yr - 1.

      // Update internal position and update Graphics
      self.xx <- (float self.cx + self.xr) * GRID
      self.yy <- (float self.cy + self.yr) * GRID
      self.Graphics.x <- self.xx
      self.Graphics.y <- self.yy
      ()

    member self.SetCoordinates(x, y) =
      self.xx <- float x
      self.yy <- float y
      self.cx <- int(self.xx / GRID)
      self.cy <- int(self.yy / GRID)
      self.xr <- (self.xx - float self.cx * GRID) / GRID
      self.yr <- (self.yy - float self.cy * GRID) / GRID

    member self.HasCollision(cx, cy) =
      if cx < 0 || cx > scene.Level.Length - 1 || cy >= scene.Level.[cx].Length then
        true
      else
        scene.Level.[cx].[cy]

    member self.OnGround () =
      self.HasCollision(self.cx, self.cy+1) && self.yr >= 0.5

  and Level1 (engine) as self =
    inherit Scene(engine)

    let rand = Random()

    member val Level : bool [] [] = [||]
    member val Blocks : Graphics = Unchecked.defaultof<Graphics> with get, set
    member val Player = Entity(self).Init() with get, set
    member val InfoText: PIXI.Text = Unchecked.defaultof<PIXI.Text> with get, set

    member self.Init () =
      self.InfoText <- PIXI.Text("Use arrows to move. \nPress R to start a new level")
      self.GraphicRoot.addChild(self.InfoText) |> ignore

      self.GenerateLevel()
      self

    member self.Update(dt: float) =
      let speed = 0.04

      if Keyboard.Manager.IsPress(Keyboard.Keys.ArrowRight) then
        self.Player.dx <- self.Player.dx + speed

      if Keyboard.Manager.IsPress(Keyboard.Keys.ArrowLeft) then
        self.Player.dx <- self.Player.dx - speed

      if Keyboard.Manager.IsPress(Keyboard.Keys.ArrowUp) && self.Player.OnGround() then
        self.Player.dy <- 0.7

      if Keyboard.Manager.IsPress(Keyboard.Keys.R) then
        self.GraphicRoot.removeChild(self.Blocks) |> ignore
        self.GenerateLevel()
        self.Player.SetCoordinates(5 * int GRID, 0)

      self.Player.Update(dt)
      ()

    member self.GenerateLevel() =
      self.Blocks <- Graphics()
      self.GraphicRoot.addChild(self.Blocks) |> ignore
      self.Blocks.beginFill(float 0x525252) |> ignore

      for x = 0 to 31 do
        self.Level.[x] <- [||]
        for y = 0 to 24 do
          self.Level.[x].[y] <- y >= 22 || y > 3 && rand.Next(100) < 30
          if self.Level.[x].[y] then
            self.Blocks.drawRect(float x * GRID, float y * GRID, GRID, GRID) |> ignore

      self.Blocks.endFill() |> ignore

  // Create and init the engine instance
  let engine = new Engine()
  engine.Init()


  let scene = Level1(engine).Init()
  engine.SetScene(scene)
  engine.Start()
