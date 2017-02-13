namespace PixiTraining.Editor

open Fable.Core.JsInterop
open Fable.Import.Browser
open PixiTraining.Editor.Core
open PixiTraining.Editor.Interactive

module Main =

  // Application code
  let app = Application()
  app.Init()

  // Build UI
  let button = Button("Click me")
  button.UI.x <- 20.
  button.Click.Add(fun (sender, evt) -> sender.Text <- "Hello !";)
   
  app.AddWidget(button)

  // Start app
  app.Start()
    