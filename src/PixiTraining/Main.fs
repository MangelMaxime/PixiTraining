namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI

open PixiTraining.Inputs
open PixiTraining.Core

open System
open System.Collections.Generic

module Main =

  let GRID = 32.

  type Position =
    { mutable cx: int
      mutable cy: int
      mutable xr: float
      mutable yr: float
      mutable xx: float
      mutable yy: float
    }

    interface IComponent

    static member Create() =
      { cx = 0
        cy = 0
        xr = 0.
        yr = 0.
        xx = 0.
        yy = 0.
      }

  type Moveable =
    { mutable dx: float
      mutable dy: float
    }

    interface IComponent

    static member Create(?dx, ?dy) =
      { dx = defaultArg dx 0.
        dy = defaultArg dy 0.
      }

  type DisplayableGraphics =
    { mutable Sprite: Graphics
    }

    interface IComponent

    static member Create(sprite) =
      { Sprite = sprite
      }

  type Collision =
    { mutable OnGround: bool
    }

    interface IComponent

    static member Create() =
      { OnGround = true
      }

  type UserControlled =
    inherit IComponent

  [<PassGenerics>]
  let addComponent<'T> comp entity =
    entity.Components.Add(typeof<'T>.Name, comp)
    entity

  let removeComponent key entity =
    entity.Components.Remove(key) |> ignore
    entity

  let createPlayer (root: Container) =
    let sprite = Graphics()
    sprite.beginFill(float 0xFFFF00) |> ignore
    sprite.drawCircle(0., 0., GRID * 0.5) |> ignore
    sprite.endFill() |> ignore
    root.addChild(sprite) |> ignore

    { Id = "player"
      Components = Dictionary<string, IComponent>()
    }
    |> addComponent<Moveable> (Moveable.Create())
    |> addComponent<Position> (Position.Create())
    |> addComponent<UserControlled> (Unchecked.defaultof<UserControlled>)
    |> addComponent<DisplayableGraphics> (DisplayableGraphics.Create(sprite))
    |> addComponent<Collision> (Collision.Create())

  let hasCollision cx cy (level: bool [] []) =
    if cx < 0 || cx > level.Length - 1 || cy >= level.[cx].Length then
      true
    else
      level.[cx].[cy]

  let generateLevel () =
    let rand = Random()
    let blocks = Graphics()
    let mutable level = [||]
    blocks.beginFill(float 0x525252) |> ignore

    for x = 0 to 31 do
      level.[x] <- [||]
      for y = 0 to 24 do
        level.[x].[y] <- y >= 22 || y > 3 && rand.Next(100) < 30
        if level.[x].[y] then
          blocks.drawRect(float x * GRID, float y * GRID, GRID, GRID) |> ignore

    blocks.endFill() |> ignore
    level, blocks

  type SystemUserInputs () =
    member val Player : Entity = Unchecked.defaultof<Entity> with get, set

    member self.SetPlayer player =
      self.Player <- player

    interface ISystem with

      member self.Update (_: float) =
        //if entity.HasComponent<UserControlled>() && entity.HasComponent<Moveable>() then
          let moveable = self.Player.GetComponent<Moveable>()
          let collision = self.Player.GetComponent<Collision>()
          let speed = 0.04

          if Keyboard.Manager.IsPress(Keyboard.Keys.ArrowRight) then
            moveable.dx <- moveable.dx + speed
          else if Keyboard.Manager.IsPress(Keyboard.Keys.ArrowLeft) then
            moveable.dx <- moveable.dx - speed

          if Keyboard.Manager.IsPress(Keyboard.Keys.ArrowUp) && collision.OnGround then
            moveable.dy <- 0.7

      member self.Init () = ()

  type SystemRenderer (entities: Entity list) =
    member val Entities : Entity list = entities with get, set

    interface ISystem with
      member self.Update (_: float) =
        for entity in self.Entities do
          if entity.HasComponent<Position>() && entity.HasComponent<DisplayableGraphics>() then
            let position = entity.GetComponent<Position>()
            let display = entity.GetComponent<DisplayableGraphics>()

            display.Sprite.x <- position.xx
            display.Sprite.y <- position.yy

      member self.Init () = ()

  type SystemPhysics (entities) =
    member val Level : bool [] [] = [||] with get, set
    member val Entities : Entity list = entities with get, set

    member self.SetLevel level =
      self.Level <- level

    interface ISystem with
      member self.Update (_: float) =
        for entity in self.Entities do
          if entity.HasComponent<Position>() && entity.HasComponent<Moveable>() then
            let position = entity.GetComponent<Position>()
            let moveable = entity.GetComponent<Moveable>()

            let frictX = 0.75
            let frictY = 0.94
            let gravity = 0.04

            // X Components
            position.xr <- position.xr + moveable.dx
            moveable.dx <- moveable.dx * frictX

            if hasCollision (position.cx - 1) position.cy self.Level && position.xr <= 0.3 then
              moveable.dx <- 0.
              position.xr <- 0.3

            if hasCollision (position.cx + 1) position.cy self.Level && position.xr >= 0.7 then
              moveable.dx <- 0.
              position.xr <- 0.7

            while position.xr < 0. do
              position.cx <- position.cx - 1
              position.xr <- position.xr + 1.

            while position.xr > 1. do
              position.cx <- position.cx + 1
              position.xr <- position.xr - 1.

            // Y components
            moveable.dy <- moveable.dy - gravity
            position.yr <- position.yr - moveable.dy
            moveable.dy <- moveable.dy * frictY

            if hasCollision position.cx (position.cy - 1) self.Level && position.yr <= 0.4 then
              moveable.dy <- 0.
              position.yr <- 0.4

            if hasCollision position.cx (position.cy + 1) self.Level && position.yr >= 0.5 then
              moveable.dy <- 0.
              position.yr <- 0.5

            while position.yr < 0. do
              position.cy <- position.cy - 1
              position.yr <- position.yr + 1.

            while position.yr > 1. do
              position.cy <- position.cy + 1
              position.yr <- position.yr - 1.

            position.xx <- (float position.cx + position.xr) * GRID
            position.yy <- (float position.cy + position.yr) * GRID

            if entity.HasComponent<Collision>() then
              let collision = entity.GetComponent<Collision>()
              collision.OnGround <- hasCollision position.cx (position.cy + 1) self.Level && position.yr >= 0.5

      member self.Init () = ()

  type WorldLevel1 (engine) =
    inherit World(engine)

    member self.Init() =
      let player = createPlayer self.Root
      // Inputs
      let systemUserInputs = SystemUserInputs()
      systemUserInputs.SetPlayer(player)

      self.Entities <-
        [ player
        ]

      // Renderer
      let systemRenderer = SystemRenderer(self.Entities)
      // Physics
      let systemPhysics = SystemPhysics(self.Entities)
      let level, blocks = generateLevel ()
      self.Root.addChild(blocks) |> ignore
      systemPhysics.SetLevel level

      self.Systems <-
        [ systemUserInputs
          systemPhysics
          systemRenderer
        ]

      base.Init()

    member self.Update (dt: float) =
      for system in self.Systems do
        system.Update(dt)


  // Create and init the engine instance
  let engine = new Engine()
  engine.Init()

  let world = WorldLevel1(engine).Init()
  engine.SetWorld(world)
  engine.Start()
