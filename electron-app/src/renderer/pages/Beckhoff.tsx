import { useState, useEffect } from 'react';
import '../App.css';
import CodeGenerator from '../components/CodeGenerator';
import ConfigModal from '../components/ConfigModal';
import { useConfig } from '../hooks/useConfig';

export default function Beckhoff() {
    const {
        draftConfig,
        updateValue,
        save,
        resetToSaved,
        resetToDefaults,
        getCLIArgs,
        isModalOpen,
        openModal,
        closeModal,
        hasUnsavedChanges,
    } = useConfig('beckhoff');

    const [direction, setDirection] = useState<string>('forward');

    // paths[0] = inputPath, paths[1] = gvlOutputPath, paths[2] = proxyOutputPath
    const callBeckhoffParserCLIForward = async (paths: string[]) => {
        const cliArgs = getCLIArgs();

        return await window.electron.ipcRenderer.runBeckhoffParserCLIForward({
            inputPath: paths[0],
            gvlOutputPath: paths[1],
            proxyOutputPath: paths[2],
            cliArgs: cliArgs,
        });
    };

    const callBeckhoffParserCLIReverse = async (paths: string[]) => {
        const cliArgs = getCLIArgs();

        return await window.electron.ipcRenderer.runBeckhoffParserCLIReverse({
            inputPath: paths[0],
            originalXMLPath: paths[1],
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
                    hasUnsavedChanges={hasUnsavedChanges}
                    config={draftConfig} //  use draft
                    onChange={updateValue} // updates draft
                    onClose={() => {
                        closeModal(); // discard draft
                    }}
                    onResetSaved={resetToSaved}
                    onResetDefaults={resetToDefaults}
                    onSubmit={async () => {
                        await save(); // commit draft → store
                        // closeModal();
                    }}
                    closeModal={closeModal}
                />
            )}
            {/* Main Content */}
            {direction === 'forward' ? (
                <CodeGenerator
                    key="forward"
                    inputFileType=".xml"
                    outputFileNames={['Gvl', 'GvlProxy']}
                    outputFileType=".cs"
                    setDirection={setDirection}
                    setModalOpen={openModal}
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
                    setModalOpen={openModal}
                    callCLI={callBeckhoffParserCLIReverse}
                />
            )}{' '}
        </>
    );
}
