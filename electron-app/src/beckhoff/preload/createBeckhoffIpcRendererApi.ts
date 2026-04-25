import type { IpcRenderer } from 'electron';
import type { RunBeckhoffCliPayload, RunBeckhoffCliResult } from '../shared/types';

export const createBeckhoffIpcRendererApi = (ipcRenderer: IpcRenderer) => ({
  runBeckhoffCLI: (payload: RunBeckhoffCliPayload): Promise<RunBeckhoffCliResult> =>
    ipcRenderer.invoke('run-beckhoff-cli', payload),
  openBeckhoffOutputLocation: (filePath: string): Promise<void> =>
    ipcRenderer.invoke('open-beckhoff-output-location', filePath),
});
