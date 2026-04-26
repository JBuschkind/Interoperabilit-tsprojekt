import '../App.css';
import CodeGenerator from '../components/CodeGenerator';
import ConfigModal from '../components/ConfigModal';
import { useConfig } from '../hooks/useConfig';

export default function Siemens() {
    const { config, updateValue, getPayload, isModalOpen, setModalOpen } =
        useConfig([
            {
                id: 'varA',
                label: 'Variable A',
                type: 'text',
                defaultValue: 'Class 1',
                value: 'Class 1',
            },
            {
                id: 'varB',
                label: 'Variable B',
                type: 'text',
                defaultValue: 'Class 2',
                value: 'Class 2',
            },
            {
                id: 'price',
                label: 'Price',
                type: 'number',
                defaultValue: 1,
                value: 1,
            },
            {
                id: 'category',
                label: 'Category',
                type: 'select',
                defaultValue: null,
                value: null,
                options: [
                    { value: 'TV', label: 'TV/Monitors' },
                    { value: 'PC', label: 'PC' },
                    { value: 'GA', label: 'Gaming/Console' },
                    { value: 'PH', label: 'Phones' },
                ],
            },
            {
                id: 'description',
                label: 'Description',
                type: 'textarea',
                defaultValue: '',
                placeholder: 'Test',
                value: '',
            },
        ]);

    // paths[0] = inputPath, paths[1] = spsOutputPath, paths[2] = spsProxyOutputPath
    const callSiemensParserCLI = async (paths: string[]) => {
        const payload = getPayload();

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
                inputFileType=".db"
                outputFileNames={['SPS', 'SPSProxy']}
                callCLI={callSiemensParserCLI}
                onConfigClick={() => setModalOpen(true)}
            />
        </>
    );
}
