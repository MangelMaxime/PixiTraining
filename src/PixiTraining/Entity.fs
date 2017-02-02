namespace PixiTraining

module Entity =

  type Entity (sprite) =
    // Base coordinates
    member val cx = 0 with get, set
    member val cy = 0 with get, set
    member val xr = 0. with get, set
    member val yr = 0. with get, set
    // Resulting coordinates
    member val xx = 0. with get, set
    member val yy = 0. with get, set
    // Graphical object
    member val sprite = sprite with get, set
    // Movements
    member val dx = 0. with get, set
    member val dy= 0. with get, set

    member self.Update() =
      ()

    member self.SetCoordinates(x, y) =
      self.xx <- x
      self.yy <- y
      self.cx <- int(self.xx / 16.)
      self.cy <- int(self.yy / 16.)
      self.xr <- (self.xx - float self.cx * 16.) / 16.
      self.yr <- (self.yy - float self.cy * 16.) / 16.
