

import { useState } from 'react';
import icon from '../../../assets/icon.svg';
import '../App.css';
import { useNavigate } from 'react-router-dom';
import { PathSelector } from '../components/PathSelector';
import {Merger} from '../components/Merger';
import Modal from '../components/Modal';
import Dropzone from '../components/Dropzone';

export default function Main() {

    const navigate = useNavigate();

    const [exportButtonLoading, setExportButtonLoading] = useState(false);
    const [mergeButtonLoading, setMergeButtonLoading] = useState(false);

    const [mergerOpen, setMergerOpen] = useState(false);
    const [modalOpen, setModalOpen] = useState<boolean>(false);

    const [originalCode, setOriginalCode] = useState<string | null>(null);
    const [modifiedCode, setModifiedCode] = useState<string | null>(null);

    const [outputPath, setOutputPath] = useState<string | null>(null);
    const [amlPath, setAmlPath] = useState<string | null>(null);


    const selectAmlFile = async () => {
        window.electron?.ipcRenderer
        const file = await window.electron.ipcRenderer.selectAmlFile();
        if (file) setAmlPath(file);
    };

    const selectOutputPath = async () => {
        const file = await window.electron.ipcRenderer.selectOutputPath();
        if (file) setOutputPath(file);
    };

    const runCLIExport = async (input: string, output: string) => {
        try {
            const result = await window.electron.ipcRenderer.runCliExport({ input, output });
            console.log("CLI Export result:", result);
            return result;
        } catch (err) {
            // TODO: Handle error
            console.error("CLI Export error:", err);
            throw err;
        }
    }

    const handleExportButton = async () => {
        if (!amlPath || !outputPath) return;

        const fileExists = await window.electron.ipcRenderer.checkFileExists(outputPath);
        if (fileExists) {
            setModalOpen(true);
            return;
        } 

        setExportButtonLoading(true);
        await runCLIExport(amlPath, outputPath);
        setExportButtonLoading(false);

        clearState();
        alert('Export completed successfully!');
    };

    const handleMerge = async () => {

        if (!amlPath || !outputPath) return

        setMergeButtonLoading(true);

        const originalCode = await window.electron.ipcRenderer.readFile(outputPath);
        setOriginalCode(originalCode);

    
        const tempOutputPath = outputPath + '.temp.cs';
        const result = await runCLIExport(amlPath, tempOutputPath);
        setModifiedCode(result.outputCode);

        setMergeButtonLoading(false);
        setModalOpen(false);
        setMergerOpen(true);
    }

    const handleAcceptMerge = async (mergedCode: string) => {
        // Write merged code to output path and clean up temp file

        if (outputPath && mergedCode) {
            await window.electron.ipcRenderer.finalizeMerge({
                outputPath,
                mergedCode
            })
            
            clearState();
            setMergerOpen(false);
            alert('Merge completed and file saved!');
        } else {
            alert('Missing output path or merged code');
        }
    };

    const handleOverwrite = async () => {
        if (!amlPath || !outputPath) return;

        setModalOpen(false);

        setExportButtonLoading(true);
        await runCLIExport(amlPath, outputPath);
        setExportButtonLoading(false);

        clearState();
        alert('Export completed successfully!');
    }

    const handleCancelMerge = () => {
        // Handle the cancelled merge
        window.electron.ipcRenderer.deleteTempFile(outputPath + '.temp.cs');
        setMergerOpen(false);
    }

    const clearState = () => {
        setAmlPath(null);
        setOutputPath(null);
        setOriginalCode(null);
        setModifiedCode(null);
    }

    return (

        

    <div className="min-h-screen flex flex-col bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow px-6 py-4">
        <div className="max-w-6xl mx-auto flex items-center justify-between">
          <h1 className="text-xl font-bold">My App</h1>
          <div>2</div>
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-1 px-6 py-8">

        <Modal
            isOpen={modalOpen}
            mergeButtonLoading={mergeButtonLoading}
            onClose={() => setModalOpen(false)}
            onMerge={handleMerge}
            onOverwrite={handleOverwrite}
        />

        {/* Open Merger */}
        {mergerOpen && originalCode && modifiedCode ? (
          <div className='flex  items-center justify-center'>
            <Merger
              originalCode={originalCode}
              modifiedCode={modifiedCode}
              onAcceptMerge={handleAcceptMerge}
              onCancelMerge={handleCancelMerge}
            />
          </div>
        ) : (
        <form className="max-w-2xl mx-auto flex flex-col gap-4 bg-gray-300 p-8 rounded-lg shadow-md">

            <div className='flex flex-col items-center justify-center '>
                <img width="300" alt="icon" src={icon} />
            </div>

            {/* AML File Selection */}
            <Dropzone
                label="Select AML file"
                accept=".aml"
                onChange={(file) => {
                    setAmlPath(window.electronApi.getFilePath(file!));
                }}
            />

            {/* Output Path Selection */}
            <PathSelector
                label="Select output path:"
                value={outputPath}
                placeholder="No folder selected"
                onSelect={selectOutputPath}
            />



            {/* Export Button */}
            <button
            type="button"
            className='bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded'
            onClick={handleExportButton}
            disabled={!amlPath || !outputPath || exportButtonLoading}
            >
              {exportButtonLoading ? 'Exporting...' : 'Export'}
            </button>

        </form>

        )}

      </main>

      {/* Footer */}
      {/* <footer className="bg-white border-t px-6 py-4">
        <div className="max-w-6xl mx-auto text-sm text-gray-500 flex justify-between">
          <span>1</span>
          <span>2</span>
        </div>
      </footer> */}

    </div>    


    );
}
