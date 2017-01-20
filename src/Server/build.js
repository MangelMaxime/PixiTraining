const path = require('path');
const fs = require('fs-extra');
const fable = require('fable-compiler');
const spawn = require('child_process').spawn;
const WebSocket = require('ws');

const BUILD_DIR = "../.."
const DEST_FILE = "server.js"
const PKG_JSON = "package.json"
const README = "README.md"
const RELEASE_NOTES = "RELEASE_NOTES.md"
const PROJ_FILE = "Server.fsproj"

const fableconfig = {
  "projFile": PROJ_FILE,
  "module": "commonjs",
  "outDir": BUILD_DIR
};

const targets = {
  clean() {
    return fable.promisify(fs.remove, path.join(BUILD_DIR, DEST_FILE))
  },
  build() {
    return this.clean()
      .then(_ => fable.compile(fableconfig))
  }
}

// As with FAKE scripts, run a default target if no one is specified
targets[process.argv[2] || "build"]().catch(err => {
  console.log(err);
  process.exit(-1);
});
