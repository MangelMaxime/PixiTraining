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

  let GRID = 32.

  type Entity (scene) as self =

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
    member val scene: Scene = scene with get, set
    // Movements
    member val dx = 0. with get, set
    member val dy= 0. with get, set

    member self.Init() =
      !!self.Graphics.beginFill(float 0xFFFF00)
      !!self.Graphics.drawCircle(0., 0., GRID * 0.5)
      !!self.Graphics.endFill()

      !!self.scene.Root.addChild(self.Graphics)
      self

    member self.Update(dt: float) =

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
      if cx < 0 || cx > scene.Level.Length || cy >= scene.Level.[cx].Length then
        true
      else
        scene.Level.[cx].[cy]

    member self.OnGround () =
      self.HasCollision(self.cx, self.cy+1) && self.yr > 0.5

  and Scene (engine) as self =
    let rand = Random()

    member val Root : Container = Container() with get
    member val MouseState = Unchecked.defaultof<Mouse.MouseState> with get, set
    member val Level : bool [] [] = [||]
    member val Blocks : Graphics = Graphics()
    member val Engine = engine with get, set
    member val Player = Entity(self).Init() with get, set

    member self.Init() =
      self.MouseState <- Mouse.init self.Root

      !!self.Root.addChild(self.Blocks)
      !!self.Blocks.beginFill(float 0x525252)

      for x = 0 to 32 do
        self.Level.[x] <- [||]
        for y = 0 to 25 do
          self.Level.[x].[y] <- y >= 22 || y > 3 && rand.Next(100) < 30
          if self.Level.[x].[y] then
            !!self.Blocks.drawRect(float x * GRID, float y * GRID, GRID, GRID)

      !!self.Blocks.endFill()
      self

    member self.Update(dt: float) =
      self.Player.Update(dt)
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
      self.Canvas.focus()

      self.Canvas.addEventListener_click(fun ev ->
        self.Canvas.focus()
        null
      )

      Browser.document.body
        .appendChild(self.Canvas) |> ignore

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
