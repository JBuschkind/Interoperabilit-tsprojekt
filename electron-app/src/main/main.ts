/* eslint global-require: off, no-console: off, promise/always-return: off */

/**
 * This module executes inside of electron's main process. You can start
 * electron renderer process from here and communicate with the other processes
 * through IPC.
 *
 * When running `npm run build` or `npm run build:main`, this file is compiled to
 * `./src/main.js` using webpack. This gives us some performance wins.
 */
import path from 'path';
import { app, BrowserWindow, shell, ipcMain, dialog, Menu } from 'electron';
import { autoUpdater } from 'electron-updater';
import log from 'electron-log';
import MenuBuilder from './menu';
import { resolveHtmlPath } from './util';
import { execFile } from 'child_process';
import { writeFileSync, existsSync, unlinkSync, readFileSync } from 'fs';
import { promisify } from 'util';

class AppUpdater {
  constructor() {
    log.transports.file.level = 'info';
    autoUpdater.logger = log;
    autoUpdater.checkForUpdatesAndNotify();
  }
}

let mainWindow: BrowserWindow | null = null;

let tempFilesToCleanUp: string[] = [];

ipcMain.on('ipc-example', async (event, arg) => {
  const msgTemplate = (pingPong: string) => `IPC test: ${pingPong}`;
  console.log(msgTemplate(arg));
  event.reply('ipc-example', msgTemplate('pong'));
});

// Changes ------------------------------------------------
ipcMain.handle('select-file-path', async (_event, filetypes: string[]) => {
  const result = await dialog.showOpenDialog({
    properties: ['openFile'],
    filters: [{ name: 'Custom File Types', extensions: filetypes }],
  });

  if (result.canceled) return null;
  return result.filePaths[0];
});

ipcMain.handle('select-dir-path', async () => {
  const result = await dialog.showOpenDialog({
    properties: ['openDirectory', 'createDirectory'],
  });

  if (result.canceled) return null;
  return result.filePaths[0];
});

ipcMain.handle('check-file-exists', async (_event, filePath: string) => {
  return existsSync(filePath);
});

ipcMain.handle('read-file', async (_event, filePath: string) => {
  if (!existsSync(filePath)) {
    throw new Error('File does not exist');
  }
  return readFileSync(filePath, 'utf-8');
});

const execFileAsync = promisify(execFile);

ipcMain.handle(
  'run-siemens-parser-cli',
  async (_event, { inputPath, spsOutputPath, spsProxyOutputPath, cliArgs }) => {
    // if output ends with .temp.cs, add it to tempFilesToCleanUp for later cleanup
    if (spsOutputPath.endsWith('.temp.cs')) {
      tempFilesToCleanUp.push(spsOutputPath);
    }
    if (spsProxyOutputPath.endsWith('.temp.cs')) {
      tempFilesToCleanUp.push(spsProxyOutputPath);
    }

    const CLI_PATH = app.isPackaged
      ? path.join(process.resourcesPath, 'CLIs/siemens/win/TIA-parser.exe')
      : path.join(__dirname, '../../CLIs/siemens/win/TIA-parser.exe');

    const { stdout } = await execFileAsync(CLI_PATH, [
      inputPath,
      spsOutputPath,
      spsProxyOutputPath,
      ...cliArgs,
    ]);
    return stdout;
  },
);

ipcMain.handle(
  'run-beckhoff-parser-cli-forward',
  async (_event, { inputPath, outputPath, cliArgs }) => {
    // if output ends with .temp.cs, add it to tempFilesToCleanUp for later cleanup
    if (outputPath.endsWith('.temp.cs')) {
      tempFilesToCleanUp.push(outputPath);
    }

    const CLI_PATH = app.isPackaged
      ? path.join(process.resourcesPath, 'CLIs/beckhoff/win/xmlParser.exe')
      : path.join(__dirname, '../../CLIs/beckhoff/win/xmlParser.exe');

    const args = [
      '--direction',
      'forward',
      '--input-xml',
      inputPath,
      '--output-cs',
      outputPath,
      '--output-txt', // Some extracted variables? TODO: Just another output file?
      path.join(
        __dirname,
        '../../CLIs/beckhoff/extra-files/extracted_variables.txt',
      ),
      '--template-xml', // Path to where the input xml will land as a reference for reverse direction. TODO: This needs to be handled cleaner
      path.join(
        __dirname,
        '../../CLIs/beckhoff/extra-files/GVL_PLC.template.xml',
      ),
      '--properties', // Path to config file
      path.join(
        __dirname,
        '../../CLIs/beckhoff/extra-files/plcstatus.properties', // Looks like CLI Arguments
      ),
    ];

    const { stdout } = await execFileAsync(CLI_PATH, args);
    return stdout;
  },
);

ipcMain.handle(
  'run-beckhoff-parser-cli-reverse',
  async (_event, { inputPath, originalXML, outputPath, cliArgs }) => {
    // if output ends with .temp.cs, add it to tempFilesToCleanUp for later cleanup
    if (outputPath.endsWith('.temp.cs')) {
      tempFilesToCleanUp.push(outputPath);
    }

    const CLI_PATH = app.isPackaged
      ? path.join(process.resourcesPath, 'CLIs/beckhoff/win/xmlParser.exe')
      : path.join(__dirname, '../../CLIs/beckhoff/win/xmlParser.exe');

    const args = [
      '--direction',
      'reverse',
      '--input-cs',
      inputPath,
      '--output-xml',
      outputPath,
      '--template-xml', // Path to the XML file that was saved in forward direction
      path.join(
        __dirname,
        '../../CLIs/beckhoff/extra-files/GVL_PLC.template.xml', // ??
      ),
    ];

    const { stdout } = await execFileAsync(CLI_PATH, args);
    return stdout;
  },
);

ipcMain.handle('finalize-merge', async (_event, { outputPath, mergedCode }) => {
  writeFileSync(outputPath, mergedCode, 'utf-8');

  const tempOutput = outputPath + '.temp.cs';
  if (existsSync(tempOutput)) {
    unlinkSync(tempOutput);
  }

  return { status: 'success' };
});

ipcMain.handle('delete-temp-file', async (_event, tempFilePath: string) => {
  if (existsSync(tempFilePath)) {
    unlinkSync(tempFilePath);
    return { status: 'deleted' };
  } else {
    return { status: 'not_found' };
  }
});

ipcMain.handle('join-path', (_event, { dir, file }) => {
  return path.join(dir, file);
});

ipcMain.handle('parse-file-path', (_event, filePath: string) => {
  const parsed = path.parse(filePath);

  return {
    dir: parsed.dir,
    name: parsed.name,
    ext: parsed.ext,
    base: parsed.base,
  };
});

ipcMain.handle('read-config', async (_event, type: string) => {
  const CONFIG_PATH = app.isPackaged
    ? path.join(process.resourcesPath, `CLIs/${type}/config.json`)
    : path.join(__dirname, `../../CLIs/${type}/config.json`);

  console.log(CONFIG_PATH);

  if (!existsSync(CONFIG_PATH)) {
    throw new Error('File does not exist');
  }
  return readFileSync(CONFIG_PATH, 'utf-8');
});

// Changes  End ------------------------------------------------

if (process.env.NODE_ENV === 'production') {
  const sourceMapSupport = require('source-map-support');
  sourceMapSupport.install();
}

const isDebug =
  process.env.NODE_ENV === 'development' || process.env.DEBUG_PROD === 'true';

if (isDebug) {
  require('electron-debug').default();
}

const installExtensions = async () => {
  const installer = require('electron-devtools-installer');
  const forceDownload = !!process.env.UPGRADE_EXTENSIONS;
  const extensions = ['REACT_DEVELOPER_TOOLS'];

  return installer
    .default(
      extensions.map((name) => installer[name]),
      forceDownload,
    )
    .catch(console.log);
};

const createWindow = async () => {
  if (isDebug) {
    await installExtensions();
  }

  const RESOURCES_PATH = app.isPackaged
    ? path.join(process.resourcesPath, 'assets')
    : path.join(__dirname, '../../assets');

  const getAssetPath = (...paths: string[]): string => {
    return path.join(RESOURCES_PATH, ...paths);
  };

  mainWindow = new BrowserWindow({
    show: false,
    width: 1024,
    height: 728,
    icon: getAssetPath('icon.png'),
    webPreferences: {
      preload: app.isPackaged
        ? path.join(__dirname, 'preload.js')
        : path.join(__dirname, '../../.erb/dll/preload.js'),
    },
  });

  mainWindow.loadURL(resolveHtmlPath('index.html'));

  mainWindow.on('ready-to-show', () => {
    if (!mainWindow) {
      throw new Error('"mainWindow" is not defined');
    }
    if (process.env.START_MINIMIZED) {
      mainWindow.minimize();
    } else {
      mainWindow.show();
    }
  });

  mainWindow.on('closed', () => {
    mainWindow = null;
  });

  // const menuBuilder = new MenuBuilder(mainWindow);
  // menuBuilder.buildMenu();
  Menu.setApplicationMenu(null);

  // Open urls in the user's browser
  mainWindow.webContents.setWindowOpenHandler((edata) => {
    shell.openExternal(edata.url);
    return { action: 'deny' };
  });

  // Remove this if your app does not use auto updates
  // eslint-disable-next-line
  new AppUpdater();
};

/**
 * Add event listeners...
 */

app.on('before-quit', () => {
  // Clean up temp files before quitting
  tempFilesToCleanUp.forEach((tempFile) => {
    if (existsSync(tempFile)) {
      unlinkSync(tempFile);
    }
  });
});

app.on('window-all-closed', () => {
  // Respect the OSX convention of having the application in memory even
  // after all windows have been closed
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app
  .whenReady()
  .then(() => {
    createWindow();
    app.on('activate', () => {
      // On macOS it's common to re-create a window in the app when the
      // dock icon is clicked and there are no other windows open.
      if (mainWindow === null) createWindow();
    });
  })
  .catch(console.log);
