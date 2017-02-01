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

  let kbState = Keyboard.init(render.canvas)

  type Player =
    { Body: Body
      mutable Ground: bool
    }


  let offset = 5.
  World.add(engine.world,
    [|
      // Walls
      Bodies.rectangle(400., -offset, 800. + 2. * offset, 50., !![ IsStatic ])
      Bodies.rectangle(400., 600. + offset, 800. + 2. * offset, 50., !![ IsStatic ])
      Bodies.rectangle(800. + offset, 300., 50., 600. + 2. * offset, !![ IsStatic ])
      Bodies.rectangle(-offset, 300., 50., 600. + 2. * offset, !![ IsStatic ])
      // Ramp
      Bodies.rectangle(600., 350., 700., 20., !![ IsStatic; Angle (-Math.PI * 0.1)])
      Bodies.rectangle(340., 580., 700., 20., !![ IsStatic; Angle (Math.PI * 0.6)])
    |]
  ) |> ignore

  let player =
    let body =
      Bodies.circle(100., 100., 25.,
        !![
          Density 0.001
          Friction 0.7
          FrictionStatic 0.
          FrictionAir 0.01
          Restitution 0.5
        ]
      )
    { Body = body
      Ground = false
    }

  World.add(engine.world, player.Body) |> ignore

  Events.on_collisionStart(engine, JsFunc1(fun ev ->
    let pairs = ev.pairs
    Browser.console.log "ijoijoi"
  ))

  Engine.run(engine)
  Render.run(render)
