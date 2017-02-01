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

  render.canvas.id <- "game"

  let kbState = Keyboard.init(!!Browser.window)

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
      Bodies.rectangle(100., 100., 25., 80.,
        !![
          Friction 0.1
          FrictionStatic 0.1
          FrictionAir 0.01
          Slop 0.
          Inertia JS.Infinity
          InverseInertia 0.
        ]
      )
    { Body = body
      Ground = false
    }

  World.add(engine.world, player.Body) |> ignore

  Events.on_collisionStart(engine, JsFunc1(fun ev ->
    let pairs = ev.pairs
    for pair in pairs do
      if pair.bodyA = player.Body || pair.bodyB = player.Body then
        player.Ground <- true
  ))

  Events.on_collisionActive(engine, JsFunc1(fun ev ->
    let pairs = ev.pairs
    for pair in pairs do
      if pair.bodyA = player.Body || pair.bodyB = player.Body then
        player.Ground <- true
  ))

  Events.on_collisionEnd(engine, JsFunc1(fun ev ->
    let pairs = ev.pairs
    for pair in pairs do
      if pair.bodyA = player.Body || pair.bodyB = player.Body then
        player.Ground <- false
  ))

  let speed = 3.

  Events.on_beforeTick(engine, JsFunc1(fun ev ->
    Browser.console.log "okpo"
    if kbState.IsPress(Keyboard.Keys.ArrowRight) then
      let vec = player.Body.velocity
      vec.x <- speed
      Body.setVelocity(player.Body, vec)
    else if kbState.IsPress(Keyboard.Keys.ArrowLeft) then
      let vec = player.Body.velocity
      vec.x <- -speed
      Body.setVelocity(player.Body, vec)


    if kbState.IsPress(Keyboard.Keys.Space) && player.Ground then
      player.Body.force <- Vector.create(0., -0.1)
  ))

  Engine.run(engine)
  Render.run(render)
