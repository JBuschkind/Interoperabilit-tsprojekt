import '../App.css';
import CodeGenerator from '../components/CodeGenerator';
import ConfigModal from '../components/ConfigModal';
import { useConfig } from '../hooks/useConfig';
import { useEffect } from 'react';

export default function Siemens() {
    const {
        config,
        updateValue,
        setConfig,
        getCLIArgs,
        isModalOpen,
        setModalOpen,
    } = useConfig([]);

    // On mount: Read config json for settings
    useEffect(() => {
        const loadConfig = async () => {
            try {
                const fileContent =
                    await window.electron.ipcRenderer.readConfig('siemens');
                const parsed = JSON.parse(fileContent);

                setConfig(parsed);
            } catch (err) {
                console.error('Failed to load config:', err);
            }
        };

        loadConfig();
    }, []);

    // paths[0] = inputPath, paths[1] = spsOutputPath, paths[2] = spsProxyOutputPath
    const callSiemensParserCLI = async (paths: string[]) => {
        const cliArgs = getCLIArgs();

        return await window.electron.ipcRenderer.runSiemensParserCLI({
            inputPath: paths[0],
            spsOutputPath: paths[1],
            spsProxyOutputPath: paths[2],
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
                <div className="invisible bg-surface-container-low p-1 rounded-sm flex items-center gap-1 border border-outline/10 shadow-lg text-surface-inverse/60">
                    <button className="px-6 py-2 text-xs font-black uppercase tracking-widest bg-primary-inverse text-on-primary-container transition-all">
                        .db → C#
                    </button>
                    <button className="px-6 py-2 text-xs font-black uppercase tracking-widest text-on-surface-variant hover:bg-surface-container transition-all">
                        C# → .db
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
            <CodeGenerator
                inputFileType=".db"
                outputFileNames={['SPS', 'SPSProxy']}
                callCLI={callSiemensParserCLI}
            />
        </>
    );
}
