

import { useState } from 'react';
import icon from '../../../assets/icon.svg';
import '../App.css';
import { useNavigate } from 'react-router-dom';
import { PathSelector } from '../components/PathSelector';
import {Merger} from '../components/Merger';

export default function Main() {

    const navigate = useNavigate();

    // States
    const [loading, setLoading] = useState(false);
    const [mergerOpen, setMergerOpen] = useState(false);

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

    const exportFile = async () => {
        if (!amlPath || !outputPath) return;

        setLoading(true);
        try {
            const result = await window.electron.ipcRenderer.runCliExport({
                input: amlPath,
                output: outputPath,
            });

            // If file already exists, open merger with temp output path
            if (result.status === 'file exists') {

                // Read in files for merger
                setOriginalCode(result.originalCode);
                setModifiedCode(result.modifiedCode);

                setMergerOpen(true);
                return;
            }

            console.log("Export result:", result);
            
            alert('Export completed!');
        } catch (err) {
            console.error(err);
            alert('Export failed');
        } finally {
            setLoading(false);
        }
    };

    const handleAcceptMerge = async (mergedCode: string) => {
        // Write merged code to output path and clean up temp file

        if (outputPath && mergedCode) {
            await window.electron.ipcRenderer.finalizeMerge({
                outputPath,
                mergedCode
            })
            setMergerOpen(false);
            alert('Merge completed and file saved!');
        } else {
            alert('Missing output path or merged code');
        }
    };

    const handleCancelMerge = () => {
        // Handle the cancelled merge
        setMergerOpen(false);
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
            <PathSelector
                label="Select AML file:"
                value={amlPath}
                placeholder="No file selected"
                onSelect={selectAmlFile}
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
            type="submit"
            className='bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded'
            onClick={exportFile}
            disabled={!amlPath || !outputPath || loading}
            >
              {loading ? 'Exporting...' : 'Export'}
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
