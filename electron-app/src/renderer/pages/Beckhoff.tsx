import '../App.css';
import CodeGenerator from '../components/CodeGenerator';
import ConfigModal from '../components/ConfigModal';
import { useConfig } from '../hooks/useConfig';

export default function Beckhoff() {
    const { config, updateValue, getPayload, isModalOpen, setModalOpen } =
        useConfig([
            {
                id: 'varA',
                label: 'Variable A',
                type: 'text',
                defaultValue: 'Class 1',
                value: 'Class 1',
            },
        ]);

    const callBeckhoffParserCLI = async (paths: string[]) => {
        const payload = getPayload();
        // TODO
        return await window.electron.ipcRenderer.runSiemensParserCLI({
            inputPath: paths[0],
            spsOutputPath: paths[1],
            spsProxyOutputPath: paths[2],
            // config: payload
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
                    onSubmit={() => {}}
                />
            )}

            {/* Main Content */}
            <CodeGenerator
                inputFileType=".xml"
                outputFileNames={['SPS']}
                callCLI={callBeckhoffParserCLI}
                onConfigClick={() => setModalOpen(true)}
            />
        </>
    );
}
