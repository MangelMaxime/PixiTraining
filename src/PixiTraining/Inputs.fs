namespace PixiTraining

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open Fable.PowerPack
open System

module Inputs =

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

  let initMouse (container: Container) =
    let state = MouseState.Initial

    container.interactive <- true
    Browser.console.log "kodzqdz"
    container.on_click(JsFunc1(fun ev ->
      Browser.console.log "koko"
    ))
    state

