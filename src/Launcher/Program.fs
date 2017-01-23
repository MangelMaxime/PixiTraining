namespace PixiTraining.Launcher

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Electron

module Program =


  // Keep a global reference of the window object, if you don't, the window will
  // be closed automatically when the JavaScript object is garbage collected.
  let mutable mainWindow: BrowserWindow option = Option.None

  let createMainWindow () =

    let options = createEmpty<BrowserWindowOptions>
    options.width <- Some 1024.
    options.height <- Some 800.
    let window = electron.BrowserWindow.Create(options)

    let sourceDirectory = Node.path.join(Node.__dirname, "js")
    let entryFile = Node.path.join(Node.__dirname, "index.html")

    // Load the index.html of the app
    let opts = createEmpty<Node.url_types.UrlOptions>
    opts.pathname <- Some entryFile
    opts.protocol <- Some "file:"
    window.loadURL(Node.url.format(opts))

    Node.fs.watch(sourceDirectory, fun _ ->
      window.webContents.reloadIgnoringCache() |> ignore
    ) |> ignore

    // Emitted when the window is closed.
    window.on_closed(unbox(fun () ->
      // Derefence the window object
      mainWindow <- Option.None
    )) |> ignore

    window.maximize()
    mainWindow <- Some window

  // Create the main window when electron app is ready
  electron.app.on_ready(unbox createMainWindow) |> ignore

    // On OS X it is common for applications and their menu bar
    // to stay active until the user quits explicitly with Cmd + Q
  electron.app.``on_window-all-closed``(unbox(fun () ->
    if Node.``process``.platform <> "darwin" then
      electron.app.quit()
  )) |> ignore

  electron.app.on_activate(unbox(fun () ->
    // On OS X it's common to re-create a window in the app when the
    // dock icon is clicked and there are no other windows open.
    if mainWindow.IsNone then
      createMainWindow()
  )) |> ignore
