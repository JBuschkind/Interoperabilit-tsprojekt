import { useState } from 'react';
import '../App.css';
import CodeGenerator from '../components/CodeGenerator';
import ConfigModal from '../components/ConfigModal';
import { useConfig } from '../hooks/useConfig';

export default function Beckhoff() {
    const { config, updateValue, getCLIArgs, isModalOpen, setModalOpen } =
        useConfig([
            {
                id: 'varA',
                label: 'Variable A',
                type: 'text',
                defaultValue: 'Class 1',
                value: 'Class 1',
            },
        ]);

    const [direction, setDirection] = useState<string>('forward');

    const callBeckhoffParserCLI = async (paths: string[]) => {
        const payload = getCLIArgs();
        // TODO
        return await window.electron.ipcRenderer.runBeckhoffParserCLI({
            inputPath: paths[0],
            outputPath: paths[1],
            direction: direction,
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
                    onSubmit={() => setModalOpen(false)} // TODO
                />
            )}
            {/* TODO: This switch is only temporary until we decide on how the UI looks lie */}
            <div className="flex flex-col items-center mt-15 gap-1">
                <label className="flex justify-center items-center cursor-pointer">
                    <span className="select-none text-sm font-medium text-heading">
                        Forward
                    </span>

                    <input
                        type="checkbox"
                        className="sr-only peer"
                        checked={direction === 'reverse'}
                        onChange={(e) =>
                            setDirection(
                                e.target.checked ? 'reverse' : 'forward',
                            )
                        }
                    />

                    <div className="relative mx-3 w-9 h-5 bg-neutral-quaternary peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-brand-soft dark:peer-focus:ring-brand-soft rounded-full peer peer-checked:after:translate-x-full rtl:peer-checked:after:-translate-x-full peer-checked:after:border-buffer after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-brand"></div>

                    <span className="select-none text-sm font-medium text-heading">
                        Reverse
                    </span>
                </label>

                <span className="text-xs text-neutral-500">
                    (Temporary toggle for testing purposes)
                </span>
            </div>
            {/* Main Content */}
            {direction === 'forward' ? (
                <CodeGenerator
                    key="forward"
                    inputFileType=".xml"
                    outputFileNames={['PlcStatusControl']}
                    outputFileType=".cs"
                    callCLI={callBeckhoffParserCLI}
                    onConfigClick={() => setModalOpen(true)}
                />
            ) : (
                <CodeGenerator
                    key="reverse"
                    inputFileType=".cs"
                    outputFileNames={['GVL_PLC.updated']}
                    outputFileType=".xml"
                    callCLI={callBeckhoffParserCLI}
                    onConfigClick={() => setModalOpen(true)}
                />
            )}{' '}
        </>
    );
}
