import { useMemo, useState } from 'react';
import type {
  BeckhoffDirection,
  RunBeckhoffCliResult,
} from '../shared/types';

type OutputPreview = {
  path: string;
  content: string;
};

const directionOptions: Array<{ value: BeckhoffDirection; label: string }> = [
  { value: 'forward', label: 'XML -> C#' },
  { value: 'reverse', label: 'C# -> XML' },
];

export default function BeckhoffTranslator() {
  const [direction, setDirection] = useState<BeckhoffDirection>('forward');
  const [sourceFilePath, setSourceFilePath] = useState<string | null>(null);
  const [result, setResult] = useState<RunBeckhoffCliResult | null>(null);
  const [outputPreview, setOutputPreview] = useState<OutputPreview | null>(null);
  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const selectedExtensions = useMemo(
    () => (direction === 'forward' ? ['xml'] : ['cs']),
    [direction],
  );

  const selectSourceFile = async () => {
    const selectedPath = await window.electron.ipcRenderer.selectFilePath(
      selectedExtensions,
    );

    if (!selectedPath) {
      return;
    }

    setSourceFilePath(selectedPath);
    setResult(null);
    setOutputPreview(null);
    setErrorMessage(null);
  };

  const runTranslation = async () => {
    if (!sourceFilePath) {
      setErrorMessage('Bitte zuerst eine Datei auswaehlen.');
      return;
    }

    setLoading(true);
    setErrorMessage(null);
    setResult(null);
    setOutputPreview(null);

    try {
      const cliResult = await window.electron.ipcRenderer.runBeckhoffCLI({
        direction,
        sourceFilePath,
      });
      setResult(cliResult);
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Unbekannter Fehler';
      setErrorMessage(message);
    } finally {
      setLoading(false);
    }
  };

  const loadOutputPreview = async (filePath: string) => {
    const content = await window.electron.ipcRenderer.readFile(filePath);
    setOutputPreview({ path: filePath, content });
  };

  const openOutputLocation = async (filePath: string) => {
    try {
      await window.electron.ipcRenderer.openBeckhoffOutputLocation(filePath);
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Ordner konnte nicht geoeffnet werden';
      setErrorMessage(message);
    }
  };

  return (
    <div className="max-w-6xl mx-auto bg-gray-300 p-8 rounded-lg shadow-md flex flex-col gap-6">
      <h2 className="text-2xl font-bold">Beckhoff Translator</h2>

      <div className="flex flex-wrap gap-3">
        {directionOptions.map((option) => (
          <button
            key={option.value}
            type="button"
            onClick={() => setDirection(option.value)}
            className={`px-4 py-2 rounded font-semibold ${
              direction === option.value
                ? 'bg-blue-600 text-white'
                : 'bg-white text-gray-700'
            }`}
          >
            {option.label}
          </button>
        ))}
      </div>

      <div className="flex flex-col gap-3">
        <button
          type="button"
          onClick={selectSourceFile}
          className="bg-white text-gray-900 border border-gray-300 rounded px-4 py-3 text-left"
        >
          {sourceFilePath ??
            (direction === 'forward'
              ? 'XML-Datei auswaehlen'
              : 'C#-Datei auswaehlen')}
        </button>

        <button
          type="button"
          onClick={runTranslation}
          disabled={!sourceFilePath || loading}
          className="bg-blue-500 hover:bg-blue-700 disabled:bg-gray-400 text-white font-bold py-2 px-4 rounded"
        >
          {loading ? 'Uebersetze...' : 'Uebersetzen'}
        </button>
      </div>

      {errorMessage && (
        <div className="bg-red-100 text-red-700 border border-red-300 rounded p-3 whitespace-pre-wrap">
          {errorMessage}
        </div>
      )}

      {result && (
        <div className="bg-white rounded p-4 border border-gray-200 flex flex-col gap-3">
          <div>
            <div className="font-semibold">
              {result.direction === 'forward'
                ? 'Generierte C# Datei'
                : 'Generierte XML Datei'}
            </div>
            <div className="flex flex-col gap-2 mt-2">
              {result.generatedFiles.map((filePath) => (
                <div
                  key={filePath}
                  className="border border-gray-200 rounded p-2 flex flex-col gap-2"
                >
                  <div className="text-sm break-all">{filePath}</div>
                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => loadOutputPreview(filePath)}
                      className="bg-gray-100 hover:bg-gray-200 text-gray-800 px-3 py-1 rounded"
                    >
                      Datei anzeigen
                    </button>
                    <button
                      type="button"
                      onClick={() => openOutputLocation(filePath)}
                      className="bg-gray-100 hover:bg-gray-200 text-gray-800 px-3 py-1 rounded"
                    >
                      Speicherort oeffnen
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {result.stdout && (
            <div>
              <div className="font-semibold">CLI Ausgabe</div>
              <pre className="text-xs bg-gray-100 p-2 rounded overflow-auto whitespace-pre-wrap">
                {result.stdout}
              </pre>
            </div>
          )}

          {result.stderr && (
            <div>
              <div className="font-semibold">CLI Fehlerausgabe</div>
              <pre className="text-xs bg-gray-100 p-2 rounded overflow-auto whitespace-pre-wrap">
                {result.stderr}
              </pre>
            </div>
          )}
        </div>
      )}

      {outputPreview && (
        <div className="bg-white rounded p-4 border border-gray-200 flex flex-col gap-2">
          <div className="font-semibold break-all">{outputPreview.path}</div>
          <pre className="text-xs bg-gray-100 p-2 rounded overflow-auto whitespace-pre-wrap max-h-96">
            {outputPreview.content}
          </pre>
        </div>
      )}
    </div>
  );
}
