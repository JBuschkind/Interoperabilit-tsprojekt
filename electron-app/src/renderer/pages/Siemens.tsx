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

            {/* Main Content */}
            <CodeGenerator
                inputFileType=".db"
                outputFileNames={['SPS', 'SPSProxy']}
                callCLI={callSiemensParserCLI}
                onConfigClick={() => setModalOpen(true)}
            />
        </>
    );
}
