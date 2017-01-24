namespace PixiTraining.Launcher

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Electron
open Fable.Import.Node

module Server =

  let createServer () =
    http.createServer(JsFunc2(fun req res ->
      res.``end``("Coucou tu vas bien ?", JS.Function.Create())
    ))



