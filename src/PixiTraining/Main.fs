namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI

module Main =

  let options =
    [ BackgroundColor (float 0x1099bb)
      Resolution 1. 
    ]

  let renderer =
    Globals.autoDetectRenderer(800., 600., options)
    |> unbox<SystemRenderer>

  let gameDiv = Browser.document.getElementById "game"

  gameDiv.appendChild(renderer.view) |> ignore

  let stage = Container()

  let texture = Texture.fromImage("static/assets/bunnu.png")

  let femaleSpriteSheets = Sprite(texture)

  femaleSpriteSheets.anchor.x <- 0.5
  femaleSpriteSheets.anchor.y <- 0.5

  femaleSpriteSheets.position.x <- 200.
  femaleSpriteSheets.position.y <- 100.
  
  stage.addChild(femaleSpriteSheets) |> ignore

  let rec animate (dt:float) =
    Browser.window.requestAnimationFrame(Browser.FrameRequestCallback animate) |> ignore

    renderer.render(stage)

  animate 0.
  