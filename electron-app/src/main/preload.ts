// Disable no-unused-vars, broken for spread args
/* eslint no-unused-vars: off */
import {
  contextBridge,
  ipcRenderer,
  IpcRendererEvent,
  webUtils,
} from 'electron';
import { read } from 'fs';

// allowed IPC channels
export type Channels = 'ipc-example';

const electronHandler = {
  ipcRenderer: {
    // <--- This object is what React will see: window.electron.ipcRenderer

    // Send a message to main process (one way)
    // In React: window.electron.ipcRenderer.sendMessage('ipc-example', 'hello');
    sendMessage(channel: Channels, ...args: unknown[]) {
      ipcRenderer.send(channel, ...args);
    },
    // Subscribes to messages from main process
    on(channel: Channels, func: (...args: unknown[]) => void) {
      const subscription = (_event: IpcRendererEvent, ...args: unknown[]) =>
        func(...args);
      ipcRenderer.on(channel, subscription);

      return () => {
        ipcRenderer.removeListener(channel, subscription);
      };
    },
    // Subscribes to a single message from main process
    once(channel: Channels, func: (...args: unknown[]) => void) {
      ipcRenderer.once(channel, (_event, ...args) => func(...args));
    },

    // generic invoke
    invoke(channel: string, ...args: unknown[]) {
      return ipcRenderer.invoke(channel, ...args);
    },

    selectFilePath: (filetypes: string[]) =>
      ipcRenderer.invoke('select-file-path', filetypes),

    selectDirPath: () => ipcRenderer.invoke('select-dir-path'),

    checkFileExists: (filePath: string) =>
      ipcRenderer.invoke('check-file-exists', filePath),

    readFile: (filePath: string) => ipcRenderer.invoke('read-file', filePath),

    runSiemensParserCLI: (payload: {
      inputPath: string;
      spsOutputPath: string;
      spsProxyOutputPath: string;
      cliArgs: string[];
    }) => ipcRenderer.invoke('run-siemens-parser-cli', payload),

    runBeckhoffParserCLIForward: (payload: {
      inputPath: string;
      outputPath: string;
      cliArgs: string[];
    }) => ipcRenderer.invoke('run-beckhoff-parser-cli-forward', payload),

    runBeckhoffParserCLIReverse: (payload: {
      inputPath: string;
      outputPath: string;
      originalXML: string;
      cliArgs: string[];
    }) => ipcRenderer.invoke('run-beckhoff-parser-cli-reverse', payload),

    finalizeMerge: (payload: { outputPath: string; mergedCode: string }) =>
      ipcRenderer.invoke('finalize-merge', payload),

    deleteTempFile: (tempFilePath: string) =>
      ipcRenderer.invoke('delete-temp-file', tempFilePath),

    joinPath: (dir: string, file: string) =>
      ipcRenderer.invoke('join-path', { dir, file }),

    parseFilePath: (filePath: string) =>
      ipcRenderer.invoke('parse-file-path', filePath),

    readConfig: (type: string) => ipcRenderer.invoke('read-config', type),
  },
};

// Creates: window.electron = electronHandler (so React can use it)
contextBridge.exposeInMainWorld('electron', electronHandler);

// For drag and drop file support in renderer process
// https://www.electronjs.org/docs/latest/api/web-utils
//
contextBridge.exposeInMainWorld('electronApi', {
  getFilePath(file: File) {
    const path = webUtils.getPathForFile(file);
    // Do something with the path, e.g., send it over IPC to the main process.
    // It's best not to expose the full file path to the web content if possible.
    return path;
  },
});

export type ElectronHandler = typeof electronHandler;
