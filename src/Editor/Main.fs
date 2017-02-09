namespace PixiTraining.Launcher

open Fable.Core
open Fable.Import
open Fable.Arch
open Fable.Arch.App.AppApi
open Fable.Arch.Html

module Main =
  
  type Model =
    {
      Temp: string
    }

    static member Initial =
      {
        Temp = ""
      }

  type Actions =
    | NoOp

  let update model actions =
    match actions with
    | NoOp -> model, []

  let view model =
    div
      []
      [ text "app running" ]

  createApp Model.Initial view update Virtualdom.createRender
  |> withStartNodeSelector "#editor"
  |> start