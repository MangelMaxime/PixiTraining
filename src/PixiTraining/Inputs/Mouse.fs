namespace PixiTraining.Inputs

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open Fable.PowerPack
open System

module Mouse =

  type ButtonState = bool

  type MouseState =
    { mutable X: float
      mutable Y: float
      mutable Left: ButtonState
      mutable Right: ButtonState
      mutable Middle: ButtonState
    }

   /// Initial state of Mouse
    static member Initial =
      { X = 0.
        Y = 0.
        Left = false
        Right = false
        Middle = false
      }

    member self.Position() =
      Point(self.X, self.Y)

  [<PassGenerics>]
  let getOriginalEvent<'T> (ev: InteractionEvent) =
    (unbox<interaction.InteractionData> ev.data).originalEvent
    |> unbox<'T>

  let init (container: Container) =
    let state = MouseState.Initial

    container.interactive <- true

    container.on_mousedown(JsFunc1(fun ev ->

      let mouseEvent = getOriginalEvent<Browser.MouseEvent> ev

      match mouseEvent.which with
      | 1. -> state.Left <- true
      | 2. -> state.Middle <- true
      | 3. -> state.Right <- true
      | _ -> failwith "Not supported button"
    )) |> ignore

    container.on_mouseup(JsFunc1(fun ev ->
      let mouseEvent = getOriginalEvent<Browser.MouseEvent> ev
      match mouseEvent.which with
      | 1. -> state.Left <- false
      | 2. -> state.Middle <- false
      | 3. -> state.Right <- false
      | _ -> failwith "Not supported button"
    )) |> ignore

    container.on("mousemove", unbox(fun (ev: InteractionEvent) ->
      let mouseEvent = getOriginalEvent<Browser.MouseEvent> ev
      state.X <- mouseEvent.offsetX
      state.Y <- mouseEvent.offsetY
    )) |> ignore

    state
