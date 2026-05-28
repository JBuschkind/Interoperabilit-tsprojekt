import { useEffect, useRef, useState } from 'react';
import icon from '../../../assets/icon.svg';
import Dropzone from './Dropzone';
import { PathSelector } from './PathSelector';
import Modal from './Modal';
import { Merger } from './Merger';
import { OverwritePreview } from './OverwritePreview';
import MiniDropzone from './MiniDropzone';
import { OutputCard } from './OutputCard';
import { Button } from './Button';
import { Toast } from './Toast';

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
        PreviewOverwrite, // approve/cancel for XML overwrite (Beckhoff reverse)
    }

    // Button loading states
    const [exportButtonLoading, setExportButtonLoading] = useState(false);
    const [acceptButtonLoading, setAcceptButtonLoading] = useState(false);

    // Toast state
    const [showToast, setShowToast] = useState(false);
    const [toastMessage, setToastMessage] = useState('');
    const [toastType, setToastType] = useState<'success' | 'error'>('success');

    // Error states for input validation
    const [inputFileError, setInputFileError] = useState<boolean>(false);
    const [outputDirError, setOutputDirError] = useState<boolean>(false);
    const [outputFilesError, setOutputFilesError] = useState<boolean>(false);

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
    const [mergeCancelDialogOpen, setMergeCancelDialogOpen] = useState(false);
    // Once true, merge UI stays mounted but toggled with `hidden` (mismerge #25 keep-alive).
    const [mergeShellMounted, setMergeShellMounted] = useState(false);
    const lastMergeTaskRef = useRef<OutputFile | null>(null);
    const [uiState, setUiState] = useState<UIState>(UIState.Idle);

    useEffect(() => {
        if (currentTask) {
            lastMergeTaskRef.current = currentTask;
        }
    }, [currentTask]);
    const [outputIsDirectory, setOutputIsDirectory] = useState<boolean>(
        direction === 'forward' ? true : false,
    );

    // Preview state for XML overwrite (Beckhoff reverse)
    const [previewFile, setPreviewFile] = useState<OutputFile | null>(null);

    /*
     * Functions
     */

    const selectOutputDirPath = async () => {
        const file = await window.electron.ipcRenderer.selectDirPath();
        if (file) {
            setOutputDirPath(file);
            setOutputDirError(false);
        }
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

        if (file) {
            setInputFileError(false);
        }
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

            if (file) {
                setOutputFilesError(false);
            }
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
        // Clear previous errors
        setInputFileError(false);
        setOutputDirError(false);
        setOutputFilesError(false);

        let hasErrors = false;

        if (!inputFile.filePath) {
            setInputFileError(true);
            setToastMessage('Please select an input file.');
            hasErrors = true;
        }
        if (outputIsDirectory && !outputDirPath) {
            setOutputDirError(true);
            if (hasErrors) {
                setToastMessage('Please select an input file and output directory.');
            } else {
                setToastMessage('Please select an output directory.');
                hasErrors = true;
            }
        }
        if (!outputIsDirectory && outputFiles.some((f) => f.file === null)) {
            setOutputFilesError(true);
            if (hasErrors) {
                setToastMessage('Please select an input file and all output files.');
            } else {
                setToastMessage('Please select all output files.');
                hasErrors = true;
            }
        }

        if (hasErrors) {
            setToastType('error');
            setShowToast(true);
            return;
        }

        // Set filePath for output files that don't have a file selected
        const updatedOutputFiles = await resolveFilePahts(
            outputFiles,
            outputDirPath,
        );

        setOutputFiles(updatedOutputFiles);

        // Reverse direction (C# → XML): skip merge, show approve/cancel preview
        if (!outputIsDirectory && direction === 'reverse') {
            setExportButtonLoading(true);
            try {
                const templateInputPath = updatedOutputFiles[0].filePath;
                if (!templateInputPath || !inputFile.filePath) return;

                const outputPaths = updatedOutputFiles
                    .map((f) => f.tempFilePath ?? f.outputPath)
                    .filter((p): p is string => typeof p === 'string');

                await callCLI([inputFile.filePath, templateInputPath, ...outputPaths]);

                const tempPath = updatedOutputFiles[0].tempFilePath;
                if (!tempPath) return;

                const generatedCode =
                    await window.electron.ipcRenderer.readFile(tempPath);

                const fileWithGenerated = {
                    ...updatedOutputFiles[0],
                    generatedCode,
                };
                setOutputFiles(
                    updatedOutputFiles.map((f, i) =>
                        i === 0 ? fileWithGenerated : f,
                    ),
                );
                setPreviewFile(fileWithGenerated);
                setUiState(UIState.PreviewOverwrite);
            } catch (err) {
                const message =
                    err && typeof err === 'object' && 'message' in err
                        ? (err as any).message
                        : String(err);
                setToastMessage(`Code generation failed: ${message}`);
                setToastType('error');
                setShowToast(true);
            } finally {
                setExportButtonLoading(false);
            }
            return;
        }

        // If any already existing output files are selected, open the modal to start merging/overwriting
        if (!outputIsDirectory) {
            setUiState(UIState.DecideMerge);
            return;
        }

        // Extract filePaths to pass to CLI
        const outputPaths = updatedOutputFiles.map((file) => file.outputPath);

        // Call CLI with input file path and all output paths
        setExportButtonLoading(true);
        try {
            await callCLI([inputFile.filePath, ...outputPaths]);

            clearState();
            setToastMessage('Code generation completed!');
            setToastType('success');
            setShowToast(true);
            setUiState(UIState.Idle);
        } catch (err) {
            const message = err && typeof err === 'object' && 'message' in err ? (err as any).message : String(err);
            setToastMessage(`Code generation failed: ${message}`);
            setToastType('error');
            setShowToast(true);
        } finally {
            setExportButtonLoading(false);
        }
    };

    const buildMergeQueue = (files: OutputFile[]): OutputFile[] => {
        return files.filter((file) => file.toBeMerged && file.file !== null);
    };

    const handleAcceptModal = async () => {
        if (!inputFile.filePath) return; // TODO: Handle Cases

        //  Read in source code for files that will be merged
        let updatedOutputFiles: typeof outputFiles;
        try {
            updatedOutputFiles = await Promise.all(
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
        } catch (err) {
            const message = err && typeof err === 'object' && 'message' in err ? (err as any).message : String(err);
            setToastMessage(`Failed to read existing files: ${message}`);
            setToastType('error');
            setShowToast(true);
            return;
        }

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
        setAcceptButtonLoading(true);
        try {
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
        } catch (err) {
            const message = err && typeof err === 'object' && 'message' in err ? (err as any).message : String(err);
            setToastMessage(`Code generation failed: ${message}`);
            setToastType('error');
            setShowToast(true);
            return;
        } finally {
            setAcceptButtonLoading(false);
        }

        // Read generated code from temp files for files that need to be merged
        let filesWithGeneratedCode: typeof updatedOutputFiles;
        try {
            filesWithGeneratedCode = await Promise.all(
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
        } catch (err) {
            const message = err && typeof err === 'object' && 'message' in err ? (err as any).message : String(err);
            setToastMessage(`Failed to read generated files: ${message}`);
            setToastType('error');
            setShowToast(true);
            return;
        }

        setOutputFiles(filesWithGeneratedCode);

        // 3. Build queue ONLY from selected files
        const queue = buildMergeQueue(filesWithGeneratedCode);

        if (queue.length === 0) {
            clearState();
            setUiState(UIState.Idle);
            setToastMessage('Code generation completed!');
            setToastType('success');
            setShowToast(true);
            return;
        }

        setMergeQueue(queue);
        setCurrentTask(queue[0]);
        setMergeShellMounted(true);
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
                setToastMessage('All merges completed!');
                setToastType('success');
                setShowToast(true);
                return [];
            }

            setCurrentTask(rest[0]);
            return rest;
        });
    };

    const handleKeepOriginal = async () => {
        if (!currentTask?.tempFilePath) return;

        setAcceptButtonLoading(true);
        try {
            try {
                window.electron.ipcRenderer.deleteTempFile(
                    currentTask.tempFilePath,
                );
            } catch (deleteErr) {
                const delMsg =
                    deleteErr &&
                    typeof deleteErr === 'object' &&
                    'message' in deleteErr
                        ? (deleteErr as { message: string }).message
                        : String(deleteErr);
                console.error('Failed to delete temp file:', delMsg);
            }

            setToastMessage(
                `Original kept for "${currentTask.fileName}".`,
            );
            setToastType('success');
            setShowToast(true);

            goToNextTask();
        } finally {
            setAcceptButtonLoading(false);
        }
    };

    const handleAcceptMerge = async (mergedCode: string) => {
        if (!currentTask?.outputPath || !currentTask.tempFilePath) return;

        setAcceptButtonLoading(true);
        try {
            await window.electron.ipcRenderer.finalizeMerge({
                outputPath: currentTask.outputPath,
                mergedCode: mergedCode,
            });

            try {
                window.electron.ipcRenderer.deleteTempFile(currentTask.tempFilePath);
            } catch (deleteErr) {
                // Non-fatal: log and notify user but continue
                const delMsg = deleteErr && typeof deleteErr === 'object' && 'message' in deleteErr ? (deleteErr as any).message : String(deleteErr);
                console.error('Failed to delete temp file:', delMsg);
            }

            setToastMessage(`Merge completed for "${currentTask.fileName}".`);
            setToastType('success');
            setShowToast(true);

            goToNextTask();
        } catch (err) {
            const message = err && typeof err === 'object' && 'message' in err ? (err as any).message : String(err);
            setToastMessage(`Merge failed: ${message}`);
            setToastType('error');
            setShowToast(true);
        } finally {
            setAcceptButtonLoading(false);
        }
    };

    const openMergeCancelDialog = () => {
        setMergeCancelDialogOpen(true);
    };

    const closeMergeCancelDialog = () => {
        setMergeCancelDialogOpen(false);
    };

    /** Skip merge for the active file only; continue with the rest of the queue. */
    const cancelCurrentMergeOnly = () => {
        if (!currentTask?.tempFilePath) {
            closeMergeCancelDialog();
            return;
        }
        window.electron.ipcRenderer.deleteTempFile(currentTask.tempFilePath);
        closeMergeCancelDialog();

        setMergeQueue((prevQueue) => {
            const [, ...rest] = prevQueue;

            if (rest.length === 0) {
                setCurrentTask(null);
                setUiState(UIState.Idle);
                // keep mergeShellMounted true — do not unmount Merger (mismerge #25)
                setToastMessage('Merge abgebrochen.');
                setToastType('success');
                setShowToast(true);
                return [];
            }

            setCurrentTask(rest[0]);
            setToastMessage(
                `Merge für „${currentTask.fileName}“ abgebrochen. Nächste Datei …`,
            );
            setToastType('success');
            setShowToast(true);
            return rest;
        });
    };

    /** Abort merge for the current file and all remaining queued files. */
    const cancelEntireMergeQueue = () => {
        mergeQueue.forEach((task) => {
            if (task.tempFilePath) {
                window.electron.ipcRenderer.deleteTempFile(task.tempFilePath);
            }
        });
        setMergeQueue([]);
        setCurrentTask(null);
        setUiState(UIState.Idle);
        // keep mergeShellMounted true — do not unmount Merger (mismerge #25)
        closeMergeCancelDialog();
        setToastMessage('Alle ausstehenden Merges wurden abgebrochen.');
        setToastType('success');
        setShowToast(true);
    };

    const handleApproveOverwrite = async () => {
        if (!previewFile?.outputPath || !previewFile.generatedCode) return;
        setAcceptButtonLoading(true);
        try {
            await window.electron.ipcRenderer.finalizeMerge({
                outputPath: previewFile.outputPath,
                mergedCode: previewFile.generatedCode,
            });

            if (previewFile.tempFilePath) {
                try {
                    window.electron.ipcRenderer.deleteTempFile(
                        previewFile.tempFilePath,
                    );
                } catch {
                    // non-fatal
                }
            }

            setPreviewFile(null);
            clearState();
            setUiState(UIState.Idle);
            setToastMessage('File written successfully!');
            setToastType('success');
            setShowToast(true);
        } catch (err) {
            const message =
                err && typeof err === 'object' && 'message' in err
                    ? (err as any).message
                    : String(err);
            setToastMessage(`Failed to write file: ${message}`);
            setToastType('error');
            setShowToast(true);
        } finally {
            setAcceptButtonLoading(false);
        }
    };

    const handleCancelOverwrite = () => {
        if (previewFile?.tempFilePath) {
            window.electron.ipcRenderer.deleteTempFile(
                previewFile.tempFilePath,
            );
        }
        setPreviewFile(null);
        setUiState(UIState.Idle);
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
        // Clear error states
        setInputFileError(false);
        setOutputDirError(false);
        setOutputFilesError(false);

        setToastMessage('State cleared!');
        setToastType('success');
        setShowToast(true);
    };

    const mergerTask: OutputFile | null = mergeShellMounted
        ? currentTask ?? lastMergeTaskRef.current
        : null;
    const mergeUiVisible =
        uiState === UIState.Merge && currentTask !== null;

    return (
        <>
            {/* Toast Notification */}
            {showToast && (
                <Toast
                    message={toastMessage}
                    type={toastType}
                    onClose={() => setShowToast(false)}
                />
            )}

            {/* Main Content */}
            {(uiState === UIState.Idle || uiState === UIState.DecideMerge) && (
                <form className="mx-auto max-w-5xl md:w-8/12 flex flex-col gap-5 md:py-12 py-4 px-4">
                    {/* Settings Section */}
                    <div className="w-full flex justify-between">
                        <div className="h-11 w-28">{/* Spacer */}</div>

                        {/* Toggle between directions is only shown if we have setDirection (currently only beckhoff page) */}
                        {setDirection ? (
                            <div className="h-11 bg-surface-container-low p-1 rounded-sm flex items-center gap-1 border border-outline/10 shadow-lg text-textcolor/60">
                                <button
                                    type="button"
                                    onClick={() => setDirection('forward')}
                                    className={`px-6 py-2 text-xs font-black uppercase tracking-widest transition-all cursor-pointer
                                ${
                                    direction === 'forward'
                                        ? 'bg-primary-container/60'
                                        : ' hover:bg-surface-container'
                                }`}
                                >
                                    .xml → C#
                                </button>

                                <button
                                    type="button"
                                    onClick={() => setDirection('reverse')}
                                    className={`px-6 py-2 text-xs font-black uppercase tracking-widest transition-all cursor-pointer
                                ${
                                    direction === 'reverse'
                                        ? 'bg-primary-container/60'
                                        : ' hover:bg-surface-container'
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
                            disabled={direction === 'reverse'}
                            className="h-11 w-28 flex justify-center items-center gap-2 text-sm px-3 py-1.5 rounded bg-surface-container-low hover:cursor-pointer hover:bg-surface-container-high text-heading border border-outline/10 shadow-lg transition-colors disabled:opacity-40 disabled:grayscale disabled:cursor-not-allowed disabled:hover:bg-surface-container-low"
                            title="Settings"
                        >
                            <span className="material-symbols-outlined text-textcolor/60 text-lg">
                                tune
                            </span>
                            <span className="text-textcolor/60">Settings</span>
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
                                error={inputFileError}
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
                                    error={outputDirError}
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
                                            fileName={outputFile.fileName}
                                            id={`mini-dropzone-${outputFile.fileName}`}
                                            accept={outputFileType}
                                            value={outputFile.file}
                                            onChange={handleOutputFileChange(
                                                outputFile.fileName,
                                            )}
                                            error={outputFilesError}
                                        />
                                    ))}
                                </div>
                            </OutputCard>
                        </div>
                    </div>
                    {/* Export Button Section*/}
                    <div className="flex flex-row justify-center items-center gap-6 bg-surface-container-highest p-6  border-t border-primary/10">
                        <Button onClick={clearState}>Clear Workspace</Button>
                        <Button
                            variant="primary"
                            onClick={handleExportButton}
                            disabled={
                                exportButtonLoading
                            }
                        >
                            {exportButtonLoading
                                ? 'Exporting...'
                                : 'Generate Code'}
                        </Button>
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

            {mergeCancelDialogOpen && (
                <div className="fixed inset-0 z-[60] flex items-center justify-center text-textcolor">
                    <div
                        className="absolute inset-0 bg-black/30 backdrop-blur-sm"
                        onClick={closeMergeCancelDialog}
                    />
                    <div className="relative bg-surface-container-low max-w-lg rounded-xs shadow-lg p-4 md:p-6 mx-4">
                        <button
                            type="button"
                            className="absolute top-3 end-3 flex items-center justify-center hover:cursor-pointer rounded-full p-1 text-center hover:bg-surface-container-highest"
                            onClick={closeMergeCancelDialog}
                            aria-label="Schließen"
                        >
                            <span className="material-symbols-outlined">
                                close
                            </span>
                        </button>
                        <div className="flex flex-col gap-4 pt-2 pe-8">
                            <h3 className="text-lg font-semibold">
                                Merge abbrechen?
                            </h3>
                            <p className="text-textcolor/80 text-sm">
                                Möchten Sie nur die aktuelle Datei aus der
                                Warteschlange entfernen oder alle noch
                                ausstehenden Merges abbrechen?
                            </p>
                            <div className="flex flex-col sm:flex-row gap-2 sm:justify-end pt-2">
                                <Button onClick={closeMergeCancelDialog}>
                                    Zurück
                                </Button>
                                <Button onClick={cancelCurrentMergeOnly}>
                                    Nur diese Datei
                                </Button>
                                <Button
                                    variant="primary"
                                    onClick={cancelEntireMergeQueue}
                                >
                                    Gesamte Warteschlange
                                </Button>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {uiState === UIState.PreviewOverwrite && previewFile && (
                <OverwritePreview
                    fileName={`${previewFile.fileName}${outputFileType}`}
                    generatedCode={previewFile.generatedCode ?? ''}
                    onApprove={handleApproveOverwrite}
                    onCancel={handleCancelOverwrite}
                    loading={acceptButtonLoading}
                />
            )}

            {mergerTask && (
                <div
                    className={
                        mergeUiVisible
                            ? 'flex flex-col flex-1 w-full min-h-0'
                            : 'hidden'
                    }
                    aria-hidden={!mergeUiVisible}
                >
                    <Merger
                        fileName={mergerTask.fileName}
                        originalCode={mergerTask.originalCode}
                        modifiedCode={mergerTask.generatedCode}
                        onAcceptMerge={handleAcceptMerge}
                        onKeepOriginal={handleKeepOriginal}
                        onCancelMerge={openMergeCancelDialog}
                        loading={acceptButtonLoading}
                    />
                </div>
            )}
        </>
    );
}
