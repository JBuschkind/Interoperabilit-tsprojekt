import '../App.css';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import CodeGenerator from '../components/CodeGenerator';
import BeckhoffTranslator from '../../beckhoff/renderer/BeckhoffTranslator';

type ActiveTab = 'siemens' | 'beckhoff';

export default function Main() {
    const [activeTab, setActiveTab] = useState<ActiveTab>('siemens');

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
                    <div className="flex gap-2">
                        <button
                            type="button"
                            onClick={() => setActiveTab('siemens')}
                            className={`px-3 py-1 rounded ${
                                activeTab === 'siemens'
                                    ? 'bg-blue-600 text-white'
                                    : 'bg-gray-200 text-gray-700'
                            }`}
                        >
                            Siemens
                        </button>
                        <button
                            type="button"
                            onClick={() => setActiveTab('beckhoff')}
                            className={`px-3 py-1 rounded ${
                                activeTab === 'beckhoff'
                                    ? 'bg-blue-600 text-white'
                                    : 'bg-gray-200 text-gray-700'
                            }`}
                        >
                            Beckhoff
                        </button>
                    </div>
                </div>
            </header>

            {/* Main Content */}
            {activeTab === 'siemens' && (
                <CodeGenerator callCLI={callSiemensParserCLI} />
            )}
            {activeTab === 'beckhoff' && <BeckhoffTranslator />}
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
