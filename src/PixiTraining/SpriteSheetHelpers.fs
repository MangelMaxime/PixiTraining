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
  module PlayerBlue =

    let SPRITES_SHEETS_KEY = "player_blue"
    // Walks
    let SPRITE_WALK_1 = "playerBlue_walk1.png"
    let SPRITE_WALK_2 = "playerBlue_walk2.png"
    let SPRITE_WALK_3 = "playerBlue_walk3.png"
    let SPRITE_WALK_4 = "playerBlue_walk4.png"
    let SPRITE_WALK_5 = "playerBlue_walk5.png"

