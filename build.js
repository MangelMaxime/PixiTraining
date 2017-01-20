const path = require('path');
const fs = require('fs-extra');
const fable = require('fable-compiler');
const spawn = require('child_process').spawn;
const WebSocket = require('ws');

const BUILD_DIR = "build"
const JS_DIR = "js"
const DEST_FILE = "bundle.js"
const PKG_JSON = "package.json"
const README = "README.md"
const RELEASE_NOTES = "RELEASE_NOTES.md"
const PROJ_FILE = "src/PixiTraining/PixiTraining.fsproj"

const fableconfig = {
  "projFile": PROJ_FILE,
  "babelPlugins": ["transform-runtime"],
  "rollup": {
    "dest": path.join(BUILD_DIR, JS_DIR, DEST_FILE),
    "external": ["PIXI"],
    "globals": {
      "PIXI": "PIXI"
    }
  }
};

const fableconfigDev =
  Object.assign({
    "sourceMaps": true,
    "watch": true
  }, fableconfig)

const targets = {
  all() {
    console.log("Not implemented");
  },
  clean() {
    return fable.promisify(fs.remove, path.join(BUILD_DIR, JS_DIR))
  },
  build() {
    return this.clean()
      .then(_ => fable.compile(fableconfig))
  },
  dev() {
    return this.clean()
      .then(_ => fable.compile(fableconfigDev))
  },
  live() {
    const build = spawn('node', ['build.js', 'dev']);

    const ws = new WebSocket("http://localhost:8080/api/websocket");

    ws.on('error', function () {
      console.log("Socket error");
    });

    build.stdout.on('data', (data) => {
      const str = data.toString('utf8');
      if (str.startsWith('Bundled')) {
        process.stdout.write("Bundled detected\n");
        ws.send("");
      }
      process.stdout.write(str);
    });

    build.stderr.on('data', (data) => {
      process.stderr.log(`stderr: ${data}`);
    });

    build.on('close', (code) => {
      process.stdout.write(`child process exited with code ${code}\n`);
    });

    return fable.promisify(_ => build);
  }
}

// As with FAKE scripts, run a default target if no one is specified
targets[process.argv[2] || "all"]().catch(err => {
  console.log(err);
  process.exit(-1);
});
