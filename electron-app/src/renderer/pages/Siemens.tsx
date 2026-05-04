import '../App.css';
import CodeGenerator from '../components/CodeGenerator';
import ConfigModal from '../components/ConfigModal';
import { useConfig } from '../hooks/useConfig';
import { useEffect, useState } from 'react';

export default function Siemens() {
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
    } = useConfig('siemens');

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
            {/* SETTINGS MODAL */}
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
            <CodeGenerator
                inputFileType=".db"
                outputFileNames={['SPS', 'SPSProxy']}
                callCLI={callSiemensParserCLI}
                setModalOpen={openModal}
            />
        </>
    );
}
