namespace PixiTraining.Editor

open Fable.Import.PIXI
open Fable.Import.Browser
open PixiTraining.Editor.Inputs
open System

module Core =

  [<AbstractClass>]
  type Widget () =
    member val UI : DisplayObject = DisplayObject() with get, set

    // General helpers
    member self.Position pos =
      self.UI.position <- pos


  and Application () =
    member val Renderer = Unchecked.defaultof<WebGLRenderer> with get, set
    member val Canvas = Unchecked.defaultof<HTMLCanvasElement> with get, set
    member val StartDate : DateTime = DateTime.Now with get, set
    member val LastTickDate = 0. with get, set
    member val DeltaTime = 0. with get, set
    member val Widgets : Widget list = [] with get, set
    member val RootContainer = Container() with get

    member self.Init () =
      let options =
        [ BackgroundColor (float 0x9999bb)
          Resolution 1.
          Antialias true
        ]
      // Init the renderer
      self.Renderer <- WebGLRenderer(window.innerWidth, window.innerHeight, options)
      // Init the canvas
      self.Canvas <- self.Renderer.view
      self.Canvas.setAttribute("tabindex", "1")
      self.Canvas.id <- "editor"

      self.Canvas.addEventListener_click(fun ev ->
        self.Canvas.focus()
        null
      )

      document.body
        .appendChild(self.Canvas) |> ignore

      Mouse.init self.Canvas
      Keyboard.init self.Canvas true

      self.Canvas.focus()

    member self.Start() =
      self.StartDate <- DateTime.Now
      self.RequestUpdate()

    member self.RequestUpdate() =
      window.requestAnimationFrame(fun dt -> self.Update(dt)) |> ignore

    member self.Update(dt: float) =
      self.Renderer.render(self.RootContainer)
      self.RequestUpdate()

    member self.AddWidget(widget: Widget) =
      self.RootContainer.addChild(widget.UI)
      |> ignore

  let defaultTextStyle =
    [
      FontFamilly "Arial"
      FontSize 14.
      WordWrap
    ]