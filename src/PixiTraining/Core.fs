namespace PixiTraining

open Fable.Core
open Fable.Import
open Fable.Import.PIXI
open System
open System.Collections.Generic

module Core =

  type ISystem =
    abstract member Update : float -> unit
    abstract member Init : unit -> unit

  and World (engine) =
    member val Root : Container = Container() with get
    member val Engine : Engine = engine with get, set
    member val Entities : Entity list = [] with get, set
    member val Systems : ISystem list = [] with get, set

    member self.Init() =
      self

    member self.Update (_: float) = ()

  and Engine () =
    member val Renderer = Unchecked.defaultof<WebGLRenderer> with get, set
    member val Canvas = Unchecked.defaultof<Browser.HTMLCanvasElement> with get, set
    member val StartDate : DateTime = DateTime.Now with get, set
    member val LastTickDate = 0. with get, set
    member val DeltaTime = 0. with get, set
    member val World: World option = None with get, set

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
      match self.World with
      | Some World ->
          World.Update(dt)
          self.Renderer.render(World.Root)
      | None -> Browser.console.warn "No scene."
      self.RequestUpdate()

    member self.SetWorld(world) =
      self.World <- Some world

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
