// Disable no-unused-vars, broken for spread args
/* eslint no-unused-vars: off */
import { contextBridge, ipcRenderer, IpcRendererEvent } from 'electron';


// allowed IPC channels
export type Channels = 'ipc-example';

const electronHandler = {
  ipcRenderer: { // <--- This object is what React will see: window.electron.ipcRenderer

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

    selectAmlFile: () => ipcRenderer.invoke('select-aml-file'),

    selectOutputPath: () => ipcRenderer.invoke('select-output-path'),

    runCliExport: (payload: { input: string; output: string }) =>
      ipcRenderer.invoke('run-cli-export', payload),

    finalizeMerge: (payload: { outputPath: string; mergedCode: string }) =>
      ipcRenderer.invoke('finalize-merge', payload),

  },
};

// Creates: window.electron = electronHandler (so React can use it)
contextBridge.exposeInMainWorld('electron', electronHandler);

export type ElectronHandler = typeof electronHandler;
