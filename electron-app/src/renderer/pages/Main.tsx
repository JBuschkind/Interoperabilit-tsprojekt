

import { useState } from 'react';
import icon from '../../../assets/icon.svg';
import '../App.css';
import { useNavigate } from 'react-router-dom';

export default function Main() {

    const navigate = useNavigate();

    const [amlPath, setAmlPath] = useState<string | null>(null);
    const [outputPath, setOutputPath] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

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
        await window.electron.ipcRenderer.runCliExport({
            input: amlPath,
            output: outputPath,
        });
        alert('Export completed!');
        } catch (err) {
        console.error(err);
        alert('Export failed');
        } finally {
        setLoading(false);
        }
    };

    return (
        <div>
        <div className="Hello">
            <img width="300" alt="icon" src={icon} />
        </div>

        <h1>Create C# Code from AML:</h1>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <button onClick={selectAmlFile}>
            Select AML File
            </button>
            <div>{amlPath ?? 'No file selected'}</div>

            <button onClick={selectOutputPath}>
            Select Output Destination
            </button>
            <div>{outputPath ?? 'No destination selected'}</div>

            <button
            onClick={exportFile}
            disabled={!amlPath || !outputPath || loading}
            >
            {loading ? 'Exporting...' : 'Export'}
            </button>

            <button onClick={() => navigate('/merger')}>
                Go to Merger
            </button>
        </div>
        </div>
    );
}
