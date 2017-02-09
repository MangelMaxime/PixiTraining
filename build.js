const path = require('path');
const fs = require('fs-extra');
const fable = require('fable-compiler');


const PKG_JSON = "package.json"
const README = "README.md"
const RELEASE_NOTES = "RELEASE_NOTES.md"
const APP_DIR = "app"
const BASE_APP_DIR = "base_app"
const JS_DIR = "js"

const PixiTraining = {
  DEST_FILE: "game.js",
  PROJ_FILE: "src/PixiTraining/PixiTraining.fsproj"
}

const Launcher = {
  DEST_FILE: "index.js",
  PROJ_FILE: "src/Launcher/Launcher.fsproj"
}

const Editor = {
  DEST_FILE: "editor.js",
  PROJ_FILE: "src/Editor/Editor.fsproj"
}

const PixiTrainingConfig = {
  "projFile": PixiTraining.PROJ_FILE,
  "babelPlugins": ["transform-runtime"],
  "rollup": {
    "dest": path.join(APP_DIR, JS_DIR, PixiTraining.DEST_FILE),
    "external": ["PIXI"],
    "globals": {
      "PIXI": "PIXI"
    }
  }
};

const LauncherConfig = {
  "projFile": Launcher.PROJ_FILE,
  "babelPlugins": ["transform-runtime"],
  "outDir": APP_DIR,
  "module": "commonjs"
};

const EditorConfig = {
  "projFile": Editor.PROJ_FILE,
  "babelPlugins": ["transform-runtime"],
  "rollup": {
    "dest": path.join(APP_DIR, JS_DIR, Editor.DEST_FILE),
    "plugins": {
      "commonjs": {
        "namedExports": {
          "virtual-dom": [ "h", "create", "diff", "patch" ]
        }
      }
    }
  }
};

const toDevConfig = (baseConfig) =>
  Object.assign({
    //"sourceMaps": true,
    "watch": true
  }, baseConfig)

const targets = {
  clean() {
    return fable.promisify(fs.remove, APP_DIR)
  },
  setEnv() {
    return this.clean()
      .then(_ => fable.promisify(fs.copy, BASE_APP_DIR, APP_DIR))
  },
  build() {
    return this.buildLauncher()
      .then(_ => fable.compile(PixiTrainingConfig))
  },
  dev() {
    return this.buildLauncher()
      .then(_ =>
        Promise.all([
          fable.compile(toDevConfig(PixiTrainingConfig)),
          fable.compile(toDevConfig(EditorConfig))
        ])
      )
  },
  buildLauncher() {
    return this.setEnv()
      .then(_ => fable.compile(LauncherConfig))
  }
}

// As with FAKE scripts, run a default target if no one is specified
targets[process.argv[2] || "build"]().catch(err => {
  console.log(err);
  process.exit(-1);
});
