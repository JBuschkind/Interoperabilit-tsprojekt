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

    const callBeckhoffParserCLIForward = async (paths: string[]) => {
        const cliArgs = getCLIArgs();

        return await window.electron.ipcRenderer.runBeckhoffParserCLIForward({
            inputPath: paths[0],
            outputPath: paths[1],
            cliArgs: cliArgs,
        });
    };

    const callBeckhoffParserCLIReverse = async (paths: string[]) => {
        const cliArgs = getCLIArgs();

        return await window.electron.ipcRenderer.runBeckhoffParserCLIReverse({
            inputPath: paths[0],
            originalXML: paths[1],
            outputPath: paths[2],
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
            {direction === 'forward' ? (
                <CodeGenerator
                    key="forward"
                    inputFileType=".xml"
                    outputFileNames={['PlcStatusControl']}
                    outputFileType=".cs"
                    setDirection={setDirection}
                    setModalOpen={setModalOpen}
                    callCLI={callBeckhoffParserCLIForward}
                />
            ) : (
                <CodeGenerator
                    key="reverse"
                    inputFileType=".cs"
                    outputFileNames={['GVL_PLC.updated']}
                    outputFileType=".xml"
                    direction="reverse"
                    setDirection={setDirection}
                    setModalOpen={setModalOpen}
                    callCLI={callBeckhoffParserCLIReverse}
                />
            )}{' '}
        </>
    );
}
