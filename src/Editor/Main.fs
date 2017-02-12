namespace PixiTraining.Editor

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open PixiTraining.Editor.Inputs
open System

module Main =

  [<AbstractClass>]
  type Widget () =
    member val UI : DisplayObject = DisplayObject() with get, set

    abstract Init : unit -> Widget
    abstract Update : float -> unit

    default self.Init () =
      self


  and Application () =
    member val Renderer = Unchecked.defaultof<WebGLRenderer> with get, set
    member val Canvas = Unchecked.defaultof<Browser.HTMLCanvasElement> with get, set
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
      self.Renderer <- WebGLRenderer(Browser.window.innerWidth, Browser.window.innerHeight, options)
      // Init the canvas
      self.Canvas <- self.Renderer.view
      self.Canvas.setAttribute("tabindex", "1")
      self.Canvas.id <- "editor"

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
      for widget in self.Widgets do
        widget.Update(dt)

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

  type Button(text) as self =
    inherit Widget()
    do
      let container = Container()
      let g = Graphics()
      g
        .beginFill(float 0x1ABC9C)
        .drawRoundedRect(0., 0., 80., 34., 4.)
        .endFill() 
        |> ignore
      let text = PIXI.Text(text, defaultTextStyle)
      text.anchor <- Point(0.5, 0.5)
      text.x <- 80. / 2.
      text.y <- 34. / 2.
      container.addChild(g, text) |> ignore
      container.interactive <- true
      self.UI <- container

      self.UI.on_click(
        JsFunc1(
          fun ev -> 
            g
              .beginFill(float 0xAAAAAA)
              .drawRoundedRect(0., 0., 80., 34., 4.)
              .endFill()
              |> ignore
            Browser.console.log "clicked"
          )
        )
      |> ignore

    member val Text : string = text with get, set
   
    override self.Update(_: float) =
      ()



  // Application code
  let app = Application()
  app.Init()

  // Build UI
  let button = Button("Click me")
  app.AddWidget(button)

  // Start app
  app.Start()