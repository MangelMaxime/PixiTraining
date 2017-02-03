namespace PixiTraining.Inputs

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open Fable.PowerPack
open System

module Keyboard =

  // We use module + type + AutoOpen to not polluate the Keyboard module with all keys definitions
  [<AutoOpen>]
  module KeyMod =
    type KeyMod =
      | Alt
      | Control
      | Shift
      | None

  [<AutoOpen>]
  module Keys =
    type Keys =
      | Dead of int
      | Backspace
      | Enter
      | Control
      | Escape
      | Tab
      | Space
      | End
      | Home
      | ArrowLeft
      | ArrowUp
      | ArrowRight
      | ArrowDown
      | Delete
      | A
      | B
      | C
      | R

  let resolveKeyFromCode keycode =
    match keycode with
    | 8 -> Keys.Backspace
    | 9 -> Keys.Tab
    | 13 -> Keys.Enter
    | 17 -> Keys.Control
    | 27 -> Keys.Escape
    | 32 -> Keys.Space
    | 35 -> Keys.End
    | 36 -> Keys.Home
    | 37 -> Keys.ArrowLeft
    | 38 -> Keys.ArrowUp
    | 39 -> Keys.ArrowRight
    | 40 -> Keys.ArrowDown
    | 46 -> Keys.Delete
    | 65 -> Keys.A
    | 66 -> Keys.B
    | 67 -> Keys.C
    | 82 -> Keys.R
    | _ -> Keys.Dead keycode

  type Modifiers =
    { mutable Shift: bool
      mutable Control: bool
      mutable CommandLeft: bool
      mutable CommandRight: bool
      mutable Alt: bool
    }

    static member Initial =
      { Shift = false
        Control = false
        CommandLeft = false
        CommandRight = false
        Alt = false
      }

  type KeyboardState =
    { mutable KeysPressed: Set<Keys.Keys>
      mutable LastKeyCode: int
      mutable LastKeyValue: string
      mutable LastKeyIsPrintable: bool
      mutable LastKey: Keys
      Modifiers: Modifiers
    }

    static member Initial =
      { KeysPressed = Set.empty
        LastKeyCode = -1
        LastKeyValue = ""
        LastKeyIsPrintable = false
        LastKey = Keys.Dead -1
        Modifiers = Modifiers.Initial
      }

    member self.IsPress key =
      self.KeysPressed.Contains(key)

    member self.ClearLastKey () =
      self.LastKeyCode <- -1
      self.LastKeyValue <- ""
      self.LastKeyIsPrintable <- false
      self.LastKey <- Keys.Dead -1

  let init (element: Browser.HTMLElement) =
    let state = KeyboardState.Initial

    let updateModifiers (e: Browser.KeyboardEvent) =
      state.Modifiers.Alt <- e.altKey
      state.Modifiers.Shift <- e.shiftKey
      state.Modifiers.Control <- e.ctrlKey
      state.Modifiers.CommandLeft <- e.keyCode = 224.
      state.Modifiers.CommandRight <- e.keyCode = 224.

    element.addEventListener_keydown(
      fun ev ->
        let code = int ev.keyCode
        let key = resolveKeyFromCode code

        state.LastKeyValue <- ev.key
        state.LastKeyCode <- code

        // Here we try to determine if the key is printable or not
        // Should not be "Dead". Exemple first press on '^' is Dead
        // And the value should be of size [1,2] because we can add:
        // * One character at a time. Example: 'a', '!', '§'
        // * Two characters at a time. Example '^^', '^p'
        // Second case occured when pressing some keys in sequence.
        // * '^^' = '^' + '^'
        // * '^p' = '^' + 'p'
        // We also have to make sure the key is not F1..F12 so we exclude keycode range: [112,123]
        state.LastKeyIsPrintable <- 1 <= ev.key.Length && ev.key.Length <= 2 && (code < 112 || code > 123)
        state.LastKey <- key
        state.KeysPressed <- Set.add key state.KeysPressed

        updateModifiers ev
        null
    )

    element.addEventListener_keyup(fun ev ->
      let code = int ev.keyCode

      state.KeysPressed <- Set.remove (resolveKeyFromCode code) state.KeysPressed
      updateModifiers ev
      null
    )

//    container.on("keydown", unbox(fun (ev: InteractionEvent) ->
//      let keyboardEvent = unbox<Browser.KeyboardEvent> ev.data
//      let code = int keyboardEvent.keyCode
//
//      state.KeysPressed <- Set.remove code state.KeysPressed
//      updateModifiers keyboardEvent
//    )) |> ignore

    state
