import '../App.css';
import { useNavigate } from 'react-router-dom';
import { useState } from 'react';
import CodeGenerator from '../components/CodeGenerator';
import ConfigModal from '../components/ConfigModal';
import type { ConfigItem, ConfigValue } from '../../types/config';

export default function Main() {
    const [configSiemens, setConfigSiemens] = useState<ConfigItem[]>([
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

    const [configModalSiemensOpen, setConfigModalSiemensOpen] = useState(true);

    const updateConfigValue = (id: string, value: ConfigValue) => {
        setConfigSiemens((prev) =>
            prev.map((item) => (item.id === id ? { ...item, value } : item)),
        );
    };

    const saveConfig = () => {
        // TODO: We may wan to save the config permanentely
        return;
    };

    // Converts config to payload format for CLI
    const getPayloadFromConfig = () => {
        return configSiemens.reduce(
            (acc, item) => {
                acc[item.id] = item.value;
                return acc;
            },
            {} as Record<string, ConfigValue>,
        );
    };

    // paths[0] = inputPath, paths[1] = spsOutputPath, paths[2] = spsProxyOutputPath
    const callSiemensParserCLI = async (paths: string[]) => {
        const payload = getPayloadFromConfig();

        return await window.electron.ipcRenderer.runSiemensParserCLI({
            inputPath: paths[0],
            spsOutputPath: paths[1],
            spsProxyOutputPath: paths[2],
            // config: payload
        });
    };

    return (
        <div className="h-screen flex flex-col  bg-gray-50">
            {/* Header */}
            <header className="bg-white shadow px-6 py-4 shrink-0">
                <div className="max-w-6xl mx-auto flex items-center justify-between">
                    <h1 className="text-xl font-bold">My App</h1>
                    <div>2</div>
                </div>
            </header>

            {configModalSiemensOpen && (
                <ConfigModal
                    title="Settings"
                    config={configSiemens}
                    onChange={updateConfigValue}
                    onClose={() => setConfigModalSiemensOpen(false)}
                    onSubmit={saveConfig}
                />
            )}

            {/* Main Content */}
            <CodeGenerator
                callCLI={callSiemensParserCLI}
                onConfigClick={() => setConfigModalSiemensOpen(true)}
            />

            {/* Footer */}
            <footer className="bg-white border-t px-6 py-4 shrink-0">
                <div className="max-w-6xl mx-auto text-sm text-gray-500 flex justify-between">
                    <span>1</span>
                    <span>2</span>
                </div>
            </footer>
        </div>
    );
}
