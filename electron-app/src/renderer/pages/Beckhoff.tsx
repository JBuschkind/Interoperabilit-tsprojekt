import { useState, useEffect } from 'react';
import '../App.css';
import CodeGenerator from '../components/CodeGenerator';
import ConfigModal from '../components/ConfigModal';
import { useConfig } from '../hooks/useConfig';

export default function Beckhoff() {
    const {
        config,
        setConfig,
        updateValue,
        getCLIArgs,
        isModalOpen,
        setModalOpen,
    } = useConfig([]);

    const [direction, setDirection] = useState<string>('forward');

    // On mount: Read config json for settings
    useEffect(() => {
        const loadConfig = async () => {
            try {
                const fileContent =
                    await window.electron.ipcRenderer.readConfig('beckhoff');
                const parsed = JSON.parse(fileContent);

                setConfig(parsed);
            } catch (err) {
                console.error('Failed to load config:', err);
            }
        };

        loadConfig();
    }, []);

    const callBeckhoffParserCLI = async (paths: string[]) => {
        const cliArgs = getCLIArgs();

        return await window.electron.ipcRenderer.runBeckhoffParserCLI({
            inputPath: paths[0],
            outputPath: paths[1],
            direction: direction,
            cliArgs: cliArgs,
        });
    };

    return (
        <>
            {/* Settings Modal */}
            {isModalOpen && (
                <ConfigModal
                    title="Settings"
                    config={config}
                    onChange={updateValue}
                    onClose={() => setModalOpen(false)}
                    onSubmit={() => setModalOpen(false)} // TODO
                />
            )}
            {/* Settings Section */}
            <div className="w-full flex justify-between">
                <div className="w-28">{/* Spacer */}</div>

                {/* Toggle between directions */}
                <div className="bg-surface-container-low p-1 rounded-sm flex items-center gap-1 border border-outline/10 shadow-lg text-surface-inverse/60">
                    <button
                        onClick={() => setDirection('forward')}
                        className={`px-6 py-2 text-xs font-black uppercase tracking-widest transition-all cursor-pointer
                        ${
                            direction === 'forward'
                                ? 'bg-primary-inverse text-on-primary-container'
                                : 'text-on-surface-variant hover:bg-surface-container'
                        }`}
                    >
                        .xml → C#
                    </button>

                    <button
                        onClick={() => setDirection('reverse')}
                        className={`px-6 py-2 text-xs font-black uppercase tracking-widest transition-all cursor-pointer
                        ${
                            direction === 'reverse'
                                ? 'bg-primary-inverse text-on-primary-container'
                                : 'text-on-surface-variant hover:bg-surface-container'
                        }`}
                    >
                        C# → .xml
                    </button>
                </div>

                {/* Settings Button */}
                <button
                    type="button"
                    onClick={() => setModalOpen(true)}
                    className="w-28 flex justify-center items-center gap-2 text-sm px-3 py-1.5 rounded bg-surface-container-low hover:cursor-pointer hover:bg-surface-container-high text-heading border border-outline/10 shadow-lg transition-colors"
                    title="Settings"
                >
                    <span className="material-symbols-outlined text-surface-inverse/60 text-lg">
                        tune
                    </span>
                    <span className="text-surface-inverse/60">Settings</span>
                </button>
            </div>
            {/* Main Content */}
            {direction === 'forward' ? (
                <CodeGenerator
                    key="forward"
                    inputFileType=".xml"
                    outputFileNames={['PlcStatusControl']}
                    outputFileType=".cs"
                    callCLI={callBeckhoffParserCLI}
                />
            ) : (
                <CodeGenerator
                    key="reverse"
                    inputFileType=".cs"
                    outputFileNames={['GVL_PLC.updated']}
                    outputFileType=".xml"
                    callCLI={callBeckhoffParserCLI}
                />
            )}{' '}
        </>
    );
}
