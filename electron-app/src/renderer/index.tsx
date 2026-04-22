import { createRoot } from 'react-dom/client';
import App from './App';

// import { loader } from "@monaco-editor/react";
// import * as monaco from "monaco-editor";
// loader.config({ monaco });

// import { loader } from "@monaco-editor/react";
// loader.config({
//   paths: {
//     // vs: "./node_modules/monaco-editor/min/vs"
//     vs: "./monaco/vs"
//   }
// });

const container = document.getElementById('root') as HTMLElement;
const root = createRoot(container);
root.render(<App />);

// calling IPC exposed from preload script
window.electron?.ipcRenderer.once('ipc-example', (arg) => {
    // eslint-disable-next-line no-console
    console.log(arg);
});
window.electron?.ipcRenderer.sendMessage('ipc-example', ['ping']);
