namespace PixiTraining.Launcher

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Node

module Server =

  let determineMimeType extname =
    match extname with
    | ".json" -> "application/json"
    | ".png" -> "image/png"
    | ".js" -> "text/javascript"
    | ".html" -> "text/html"
    | ".css" -> "text/css"
    | _ -> "text/html" // Default mime type

  [<KeyValueList>]
  type HeaderOptions =
    | [<CompiledName("Content-type")>] ContentType of string

  let createServer () =
    http.createServer(JsFunc2(fun request response ->

      let filePath =
        match request.url with
        | Some s ->
          match s with
          | "/" -> "./index.html"
          | s -> sprintf ".%s" s
        | None -> "./index.html"

      let extname = path.extname(filePath)
      let contentType = determineMimeType extname
      let absoluteFilePath = path.join(__dirname, filePath)

      fs.readFile(absoluteFilePath, JsFunc2(fun error content ->
        if error <> null then
          match error.code with
          | Some code ->
              match code with
              | "ENOENT" ->
                  response.writeHead(404., ?headers=None)
                  response.``end``()
              | _ ->
                  response.writeHead(500., ?headers=None)
                  let msg =
                    sprintf "Sorry, something went wrong.\n Error code is: %s" code
                  response.``end``(msg, "utf-8")
          | None -> failwith "Not error code found should not happen"
        else
          response.writeHead(200., [ ContentType contentType ])
          response.``end``(content, "utf-8")
      ))

    ))
