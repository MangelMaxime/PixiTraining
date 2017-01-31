namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open Fable.Import.Matter

open PixiTraining.Inputs

open System

module Main =

  let engine = Matter.Engine.create(null)

  let render =
    Matter.Render.create
      [
        Element Browser.document.body
        OptEngine engine
      ]

  // create two boxes and a ground
  let boxA = Bodies.rectangle(400., 200., 80., 80.)
  let boxB = Bodies.rectangle(450., 50., 80., 80.)
  let ground = Bodies.rectangle(400., 610., 810., 60., !![ IsStatic ])

  World.add(engine.world, [| boxA; boxB; ground |]) |> ignore

  Engine.run(engine)
  Render.run(render)

  let test =
    [| boxA, boxB, ground |]
  Browser.console.log test
