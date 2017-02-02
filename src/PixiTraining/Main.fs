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

  type Scene (engine) =
    member val Root : Container = null with get, set
    member val MouseState = Unchecked.defaultof<Mouse.MouseState> with get, set
    member val Level : bool [] [] = [||]
    member val Engine = engine with get, set

  type Engine () =
    member val Renderer = Unchecked.defaultof<WebGLRenderer> with get, set
    member val Canvas = Unchecked.defaultof<Browser.HTMLCanvasElement> with get, set
    member val StartDate : DateTime = DateTime.Now with get, set
    member val LastTickDate = 0. with get, set
    member val DeltaTime = 0. with get, set

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
      Browser.console.log "loop"

      self.RequestUpdate()


  let test =
    [|
      [| 0; 1; 2; 3 |]
      [| 10; 11; 12; 13 |]
    |]

  Browser.console.log test.[0].[2]
  Browser.console.log test.[1].[2]

  // Create and init the engine instance
  let engine = new Engine()
  engine.Init()
  engine.Start()
