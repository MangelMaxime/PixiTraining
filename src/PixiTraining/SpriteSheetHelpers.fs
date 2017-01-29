namespace PixiTraining

open Fable
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.PIXI
open Fable.PowerPack

open System

[<AutoOpen>]
module SpriteSheetHelpers =

  [<RequireQualifiedAccess>]
  module Tiles =

    let SPRITES_SHEETS_KEY = "tiles"

    let SPRITE_COUCH_MIDDLE = "couch.png"
    let SPRITE_COUCH_LEFT = "couch_left.png"
    let SPRITE_RIGHT = "couch_right.png"
    let SPRITE_DIRT_1 = "dirt_1.png"
    let SPRITE_DIRT_2 = "dirt_2.png"
    let SPRITE_GRASS_1 = "grass_1.png"
    let SPRITE_GRASS_2 = "grass_2.png"
    let SPRITE_GRASS_3 = "grass_3.png"
    let SPRITE_GRASS_4 = "grass_4.png"
    let SPRITE_GUN_BULLET = "gun_bullet.png"

  [<RequireQualifiedAccess>]
  module Survivors =

    let SPRITES_SHEETS_KEY = "survivors"

    let SPRITE_GUN = "survivor1_gun.png"
    let SPRITE_HOLD = "survivor1_hold.png"
    let SPRITE_MACHINE = "survivor1_machine.png"
    let SPRITE_RELOAD = "survivor1_reload.png"
    let SPRITE_SILENCER = "survivor1_silencer.png"
    let SPRITE_STAND = "survivor1_stand.png"
