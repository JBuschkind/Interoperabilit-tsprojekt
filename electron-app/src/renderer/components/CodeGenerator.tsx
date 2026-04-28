import { useState } from 'react';
import icon from '../../../assets/icon.svg';
import Dropzone from './Dropzone';
import { PathSelector } from './PathSelector';
import Modal from './Modal';
import { Merger } from './Merger';
import MiniDropzone from './MiniDropzone';
import { OutputCard } from './OutputCard';

type CodeGeneratorProps = {
    inputFileType?: string;
    outputFileNames?: string[];
    outputFileType?: string;
    direction?: string;
    callCLI: (args: string[]) => Promise<string>;
    setDirection?: (direction: string) => void;
    setModalOpen: (isOpen: boolean) => void;
};

export default function CodeGenerator({
    inputFileType = '.db',
    outputFileNames = ['SPS', 'SPSProxy'],
    outputFileType = '.cs',
    direction = 'forward', // e.g. C# -> .xml
    callCLI,
    setDirection,
    setModalOpen,
}: CodeGeneratorProps) {
    type InputFile = {
        fileName: string | null;
        file: File | null;
        filePath: string | null;
    };

    type OutputFile = {
        fileName: string; // are given by the props
        file: File | null; // the actual file (if one is selected in the dropzone)
        filePath: string | null; // the original file path (only set if file was selected)
        tempFilePath: string | null; // the output path for the temp file => temp file is generated code that is used for merging
        outputPath: string | null; // the path of the output
        originalCode: string | null; // the code of the selected file
        generatedCode: string | null; // the generated code based on the input file
        mergedCode: string | null; // the merged code (by user)
        toBeMerged: boolean; // true = selected file will be merged with generated file, false = selected file will be overwritten
    };

    enum UIState {
        Idle, // file inputs
        DecideMerge, // modal open
        Merge,
    }

    // Button loading states
    const [exportButtonLoading, setExportButtonLoading] = useState(false);
    const [acceptButtonLoading, setAcceptButtonLoading] = useState(false);

    // Input File (e.g. .db or .xml)
    const [inputFile, setInputFile] = useState<InputFile>({
        fileName: null,
        file: null,
        filePath: null,
    });

    // Output Files (can be reinsterted in the UI for merging/overwriting):
    const [outputFiles, setOutputFiles] = useState<OutputFile[]>(
        outputFileNames.map((name) => ({
            fileName: name,
            file: null, // Only set if we provide an already exiting output file
            filePath: null,
            tempFilePath: null,
            outputPath: null,
            originalCode: null,
            mergedCode: null,
            generatedCode: null,
            toBeMerged: true,
        })),
    );

    // The path where output files will go per default (if no old output file is selected)
    const [outputDirPath, setOutputDirPath] = useState<string | null>(null);

    // UI State
    const [mergeQueue, setMergeQueue] = useState<OutputFile[]>([]); // Contains output files that were selected for merging
    const [currentTask, setCurrentTask] = useState<OutputFile | null>(null); // The output file that is being merged currently
    const [uiState, setUiState] = useState<UIState>(UIState.Idle);
    const [outputIsDirectory, setOutputIsDirectory] = useState<boolean>(
        direction === 'forward' ? true : false,
    );

    /*
     * Functions
     */

    const selectOutputDirPath = async () => {
        const file = await window.electron.ipcRenderer.selectDirPath();
        if (file) setOutputDirPath(file);
    };

    const handleInputFileChange = async (file: File | null) => {
        let filePath = null;
        if (file) {
            const path = await window.electronApi.getFilePath(file);
            filePath = path;
        }
        setInputFile((prev) => ({
            ...prev,
            file,
            fileName: file ? file.name : null,
            filePath,
        }));
    };

    // Sets file in an output file
    const handleOutputFileChange =
        (fileName: string) => (file: File | null) => {
            setOutputFiles((prev) =>
                prev.map((outputFile) =>
                    outputFile.fileName === fileName
                        ? {
                              ...outputFile,
                              file,
                          }
                        : outputFile,
                ),
            );
        };

    // Updates the toBeMerged value based on the toggle
    const handleToggleChange = (fileName: string, value: boolean) => {
        setOutputFiles((prev) =>
            prev.map((file) =>
                file.fileName === fileName
                    ? { ...file, toBeMerged: value }
                    : file,
            ),
        );
    };

    // Sets all outputPaths for outputFiles and filePaths for outputFiles with a selected file
    const resolveFilePahts = async (
        files: OutputFile[],
        outputDirPath: string | null,
    ) => {
        return Promise.all(
            files.map(async (outputFile) => {
                // For selected old output files: Set filePath, outputPath, tempFilePath
                if (!outputIsDirectory && outputFile.file) {
                    const filePath = await window.electronApi.getFilePath(
                        outputFile.file,
                    );

                    const parsed =
                        await window.electron.ipcRenderer.parseFilePath(
                            filePath,
                        );

                    const tempFilePath =
                        await window.electron.ipcRenderer.joinPath(
                            parsed.dir,
                            `${parsed.name}.temp${parsed.ext}`,
                        );

                    return {
                        ...outputFile,
                        filePath: filePath,
                        outputPath: filePath, // The file will either be merged or overwritten
                        tempFilePath: tempFilePath,
                    };
                }
                // For outputFiles without an existing file: Set outputpath based on selected directory and filename
                if (!outputDirPath) {
                    throw new Error(
                        'Missing outputDirPath for new generated files',
                    );
                }

                const outputPath = await window.electron.ipcRenderer.joinPath(
                    outputDirPath,
                    `${outputFile.fileName}${outputFileType}`,
                );
                return {
                    ...outputFile,
                    outputPath,
                };
            }),
        );
    };

    const handleExportButton = async () => {
        if (!inputFile.filePath) return; // TODO: Show Input field feedback
        if (outputIsDirectory && !outputDirPath) return;
        if (!outputIsDirectory && outputFiles.some((f) => f.file === null))
            return;

        // Set filePath for output files that don't have a file selected
        const updatedOutputFiles = await resolveFilePahts(
            outputFiles,
            outputDirPath,
        );

        setOutputFiles(updatedOutputFiles);

        // If any already exiting output files are selected, open the modal to start merging/overwriting
        if (!outputIsDirectory) {
            setUiState(UIState.DecideMerge);
            return;
        }

        // Extract filePaths to pass to CLI
        const outputPaths = updatedOutputFiles.map((file) => file.outputPath);

        // Call CLI with input file path and all output paths
        await callCLI([inputFile.filePath, ...outputPaths]);

        clearState();
        setUiState(UIState.Idle);
    };

    const buildMergeQueue = (files: OutputFile[]): OutputFile[] => {
        return files.filter((file) => file.toBeMerged && file.file !== null);
    };

    const handleAcceptModal = async () => {
        if (!inputFile.filePath) return; // TODO: Handle Cases

        //  Read in source code for files that will be merged
        const updatedOutputFiles = await Promise.all(
            outputFiles.map(async (file) => {
                if (file.toBeMerged && file.filePath) {
                    const originalCode =
                        await window.electron.ipcRenderer.readFile(
                            file.filePath,
                        );

                    return {
                        ...file,
                        originalCode,
                    };
                }

                return file;
            }),
        );

        setOutputFiles(updatedOutputFiles);

        // Get output paths: tempFilePath for files to be merged, outputPath for others
        // TODO: this output path extraction could be handled in parent commponent
        const outputPaths = updatedOutputFiles
            .map((outputFile) =>
                outputFile.toBeMerged && outputFile.tempFilePath
                    ? outputFile.tempFilePath
                    : outputFile.outputPath,
            )
            .filter((path): path is string => typeof path === 'string'); // should always be string anyway

        // Special Case: If we do C# -> .XML, we need the original XML as a second input as well.
        // This input however will be selected as a merging file
        if (direction === 'reverse' && updatedOutputFiles[0].filePath) {
            const templateInputPath = updatedOutputFiles[0].filePath;
            await callCLI([
                inputFile.filePath,
                templateInputPath,
                ...outputPaths,
            ]);
        } else {
            // Call CLI with input file path and all output paths (writes either temp files or final output files, can be mixed)
            await callCLI([inputFile.filePath, ...outputPaths]);
        }

        // Read generated code from temp files for files that need to be merged
        const filesWithGeneratedCode = await Promise.all(
            updatedOutputFiles.map(async (outputFile) => {
                if (outputFile.toBeMerged && outputFile.tempFilePath) {
                    const generatedCode =
                        await window.electron.ipcRenderer.readFile(
                            outputFile.tempFilePath,
                        );
                    return { ...outputFile, generatedCode };
                }
                return outputFile;
            }),
        );

        setOutputFiles(filesWithGeneratedCode);

        // 3. Build queue ONLY from selected files
        const queue = buildMergeQueue(filesWithGeneratedCode);

        if (queue.length === 0) {
            clearState();
            setUiState(UIState.Idle);
            return;
        }

        setMergeQueue(queue);
        setCurrentTask(queue[0]);
        setUiState(UIState.Merge);
    };

    const goToNextTask = () => {
        setMergeQueue((prevQueue) => {
            const [, ...rest] = prevQueue;

            if (rest.length === 0) {
                // done
                setCurrentTask(null);
                setUiState(UIState.Idle);
                clearState();
                alert('All merges completed!');
                return [];
            }

            setCurrentTask(rest[0]);
            return rest;
        });
    };

    const handleAcceptMerge = async (mergedCode: string) => {
        if (!currentTask?.outputPath || !currentTask.tempFilePath) return;

        await window.electron.ipcRenderer.finalizeMerge({
            outputPath: currentTask.outputPath,
            mergedCode: mergedCode,
        });

        window.electron.ipcRenderer.deleteTempFile(currentTask.tempFilePath);

        goToNextTask();
    };

    const handleCancelMerge = () => {
        // TODO:
        // 1. Open Modal
        // 2. Rollback ?
        // Delete temp files

        return;
        // window.electron.ipcRenderer.deleteTempFile(outputPath + '.temp.cs');
        // setMergeQueue([]);
        // setCurrentTask(null);
        // setUiState(UIState.Idle);
    };

    const handleSkipMerge = () => {
        // TODO: Maybe its nice to have a skip button that just keeps the original code (even though this could be accomblished used merging)
        // -> Use Case: You started merging but realized in the middle that you want to keep the orginal file
        // Same could be done for overwrite -> Keep all the generated file
        return;
    };

    const handleConfigClick = () => {
        return;
    };

    // Resets all States
    const clearState = () => {
        setInputFile({
            fileName: null,
            file: null,
            filePath: null,
        });
        setOutputDirPath(null);
        setOutputFiles(
            outputFileNames.map((name) => ({
                fileName: name,
                file: null,
                filePath: null,
                tempFilePath: null,
                outputPath: null,
                originalCode: null,
                mergedCode: null,
                generatedCode: null,
                toBeMerged: true,
            })),
        );
    };

    return (
        <>
            {/* Main Content */}
            {(uiState === UIState.Idle || uiState === UIState.DecideMerge) && (
                <form className="mx-auto max-w-5xl w-8/12 flex flex-col gap-5 md:py-12 py-4 px-4">
                    {/* Settings Section */}
                    <div className="w-full flex justify-between">
                        <div className="h-11 w-28">{/* Spacer */}</div>

                        {/* Toggle between directions is only shown if we have setDirection (currently only beckhoff page) */}
                        {setDirection ? (
                            <div className="h-11 bg-surface-container-low p-1 rounded-sm flex items-center gap-1 border border-outline/10 shadow-lg text-surface-inverse/60">
                                <button
                                    onClick={() => setDirection('forward')}
                                    className={`px-6 py-2 text-xs font-black uppercase tracking-widest transition-all cursor-pointer
                                ${
                                    direction === 'forward'
                                        ? 'bg-primary-inverse text-on-primary-container'
                                        : 'text-on-surface-variant hover:bg-surface-container'
                                }`}
                                >
                                    .xml → C#
                                </button>

                                <button
                                    onClick={() => setDirection('reverse')}
                                    className={`px-6 py-2 text-xs font-black uppercase tracking-widest transition-all cursor-pointer
                                ${
                                    direction === 'reverse'
                                        ? 'bg-primary-inverse text-on-primary-container'
                                        : 'text-on-surface-variant hover:bg-surface-container'
                                }`}
                                >
                                    C# → .xml
                                </button>
                            </div>
                        ) : (
                            <div className="h-11 w-28">{/* Spacer */}</div>
                        )}

                        {/* Settings Button */}
                        <button
                            type="button"
                            onClick={() => setModalOpen(true)}
                            className="h-11 w-28 flex justify-center items-center gap-2 text-sm px-3 py-1.5 rounded bg-surface-container-low hover:cursor-pointer hover:bg-surface-container-high text-heading border border-outline/10 shadow-lg transition-colors"
                            title="Settings"
                        >
                            <span className="material-symbols-outlined text-surface-inverse/60 text-lg">
                                tune
                            </span>
                            <span className="text-surface-inverse/60">
                                Settings
                            </span>
                        </button>
                    </div>
                    {/* Input section */}
                    <div className="flex flex-col gap-3">
                        {/* Section Header */}
                        <div className="flex items-center justify-between">
                            <h2 className="font-headline text-lg font-bold tracking-tight text-primary uppercase flex items-center gap-2">
                                Source Input
                            </h2>
                        </div>
                        <div className="bg-surface-container-low p-6 rounded-xs">
                            <Dropzone
                                id="input-dropzone"
                                accept={inputFileType}
                                value={inputFile.file}
                                onChange={handleInputFileChange}
                            />
                        </div>
                    </div>

                    {/* Output Section */}
                    <div className="flex flex-col gap-3">
                        {/* Section Header */}
                        <div className="flex items-center justify-between">
                            <h2 className="font-headline text-lg font-bold tracking-tight text-primary uppercase flex items-center gap-2">
                                Output Destination
                            </h2>
                        </div>
                        <div className="flex justify-between gap-6">
                            {/* Output Directory Selection */}
                            <OutputCard
                                selected={outputIsDirectory}
                                disabled={direction === 'reverse'}
                                onSelect={() => setOutputIsDirectory(true)}
                                icon="folder_zip"
                                title="Output Directory"
                                description="Batch export to target system folder"
                                name="output_mode"
                                checked={outputIsDirectory === true}
                            >
                                <PathSelector
                                    value={outputDirPath}
                                    placeholder="Select destination path..."
                                    onSelect={selectOutputDirPath}
                                />
                            </OutputCard>

                            {/* Output/Merge File Selection */}
                            <OutputCard
                                selected={!outputIsDirectory}
                                disabled={false}
                                onSelect={() => setOutputIsDirectory(false)}
                                icon="merge_type"
                                title="Link to Existing Files"
                                description="Merge or overwrite generated code into existing files"
                                name="output_mode"
                                checked={outputIsDirectory === false}
                            >
                                <div className="min-w-0 flex flex-col items-center max-h-42 overflow-y-auto scrollbar-custom pr-2 space-y-2">
                                    {outputFiles.map((outputFile) => (
                                        <MiniDropzone
                                            key={outputFile.fileName}
                                            id={`mini-dropzone-${outputFile.fileName}`}
                                            accept={outputFileType}
                                            value={outputFile.file}
                                            onChange={handleOutputFileChange(
                                                outputFile.fileName,
                                            )}
                                        />
                                    ))}
                                </div>
                            </OutputCard>
                        </div>
                    </div>
                    {/* Export Button Section*/}
                    <div className="flex flex-row justify-center items-center gap-6 bg-surface-container-highest p-6  border-t border-primary/10">
                        <button
                            type="button"
                            onClick={clearState}
                            className="border border-outline px-6 py-2.5 text-xs font-bold uppercase tracking-widest hover:bg-surface-bright hover:cursor-pointer transition-colors active:scale-95 text-surface-inverse"
                        >
                            Clear Workspace
                        </button>
                        <button
                            type="button"
                            className="
                                bg-primary text-on-primary px-10 py-2.5 text-xs font-black uppercase tracking-[0.2em]
                                shadow-lg shadow-primary/20 transition-all
                                hover:brightness-110 active:scale-95 hover:cursor-pointer
                                disabled:opacity-40 disabled:cursor-not-allowed
                                disabled:hover:brightness-100 disabled:active:scale-100
                            "
                            onClick={handleExportButton}
                            disabled={
                                !inputFile.filePath ||
                                (outputIsDirectory && !outputDirPath) ||
                                (!outputIsDirectory &&
                                    outputFiles.some((f) => f.file === null)) ||
                                exportButtonLoading
                            }
                        >
                            {exportButtonLoading
                                ? 'Exporting...'
                                : 'Generate Code'}
                        </button>
                    </div>
                </form>
            )}

            {uiState === UIState.DecideMerge && (
                <Modal
                    acceptButtonLoading={acceptButtonLoading}
                    onClose={() => setUiState(UIState.Idle)}
                    onAccept={handleAcceptModal}
                    files={outputFiles.filter(
                        (outputFile) => outputFile.file !== null, // Only files that were provide will be merged or overwritten
                    )}
                    onToggleChange={handleToggleChange}
                />
            )}

            {uiState === UIState.Merge && currentTask && (
                <Merger
                    key={currentTask.fileName}
                    fileName={currentTask.fileName}
                    originalCode={currentTask.originalCode}
                    modifiedCode={currentTask.generatedCode}
                    onAcceptMerge={handleAcceptMerge}
                    onCancelMerge={handleCancelMerge}
                />
            )}
        </>
    );
}
