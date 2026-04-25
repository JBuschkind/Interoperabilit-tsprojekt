import { ipcMain, shell } from 'electron';
import path from 'path';
import { existsSync } from 'fs';
import type { RunBeckhoffCliPayload } from '../shared/types';
import { runBeckhoffCli } from './runBeckhoffCli';

export const registerBeckhoffIpcHandlers = () => {
  ipcMain.handle('run-beckhoff-cli', async (_event, payload: RunBeckhoffCliPayload) => {
    return runBeckhoffCli(payload);
  });

  ipcMain.handle('open-beckhoff-output-location', async (_event, filePath: string) => {
    const absoluteFilePath = path.resolve(filePath);

    if (existsSync(absoluteFilePath)) {
      shell.showItemInFolder(absoluteFilePath);
      return;
    }

    const folderPath = path.dirname(absoluteFilePath);
    const openError = await shell.openPath(folderPath);

    if (openError) {
      throw new Error(openError);
    }
  });
};
