import '../App.css';
import { useNavigate } from 'react-router-dom';
import CodeGenerator from '../components/CodeGenerator';

export default function Main() {
    const callSiemensParserCLI = async (paths: string[]) => {
        // paths[0] = inputPath, paths[1] = spsOutputPath, paths[2] = spsProxyOutputPath
        return await window.electron.ipcRenderer.runSiemensParserCLI({
            inputPath: paths[0],
            spsOutputPath: paths[1],
            spsProxyOutputPath: paths[2],
        });
    };

    return (
        <div className="min-h-screen flex flex-col bg-gray-50">
            {/* Header */}
            <header className="bg-white shadow px-6 py-4">
                <div className="max-w-6xl mx-auto flex items-center justify-between">
                    <h1 className="text-xl font-bold">My App</h1>
                    <div>2</div>
                </div>
            </header>

            {/* Main Content */}
            <CodeGenerator callCLI={callSiemensParserCLI} />
            {/* Footer */}
            {/* <footer className="bg-white border-t px-6 py-4">
        <div className="max-w-6xl mx-auto text-sm text-gray-500 flex justify-between">
          <span>1</span>
          <span>2</span>
        </div>
      </footer> */}
        </div>
    );
}
