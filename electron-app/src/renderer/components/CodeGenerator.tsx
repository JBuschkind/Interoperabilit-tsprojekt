import { useState } from 'react';
import icon from '../../../assets/icon.svg';
import Dropzone from './Dropzone';
import { PathSelector } from './PathSelector';
import Modal from './Modal';
import { Merger } from './Merger';

type CodeGeneratorProps = {
    inputFileType?: string;
    parameter?: string[];
    outputFileNames?: string[];
    outputFileType?: string;
    callCLI: (args: string[]) => Promise<string>;
    onConfigClick: () => void;
};

export default function CodeGenerator({
    inputFileType = '.db',
    outputFileNames = ['SPS', 'SPSProxy'],
    outputFileType = '.cs',
    callCLI,
    onConfigClick,
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
                if (outputFile.file) {
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
        const needsOutputDir = outputFiles.some((f) => f.file === null);
        if (needsOutputDir && !outputDirPath) return; // TODO: Show Output path feedback

        // Set filePath for output files that don't have a file selected
        const updatedOutputFiles = await resolveFilePahts(
            outputFiles,
            outputDirPath,
        );

        setOutputFiles(updatedOutputFiles);

        // If any already exiting output files are selected, open the modal to start merging/overwriting
        if (outputFiles.some((outputFile) => outputFile.file !== null)) {
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
        const outputPaths = updatedOutputFiles
            .map((outputFile) =>
                outputFile.toBeMerged && outputFile.tempFilePath
                    ? outputFile.tempFilePath
                    : outputFile.outputPath,
            )
            .filter((path): path is string => typeof path === 'string'); // should always be string anyway

        // Call CLI with input file path and all output paths (writes either temp files or final output files, can be mixed)
        await callCLI([inputFile.filePath, ...outputPaths]);

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
        <div className="flex-1 flex flex-col justify-center items-center">
            {/* Main Content */}
            {(uiState === UIState.Idle || uiState === UIState.DecideMerge) && (
                <form className="max-w-7xl flex flex-col gap-4 bg-gray-300 p-8 rounded-lg shadow-md">
                    <div className="flex justify-end">
                        <button
                            type="button"
                            onClick={onConfigClick}
                            className="text-sm px-3 py-1.5 rounded-base bg-neutral-secondary-medium hover:cursor-pointer hover:bg-neutral-tertiary-medium text-heading border border-default-medium"
                        >
                            Settings
                        </button>
                    </div>

                    <div className="flex flex-col items-center justify-center ">
                        <img width="300" alt="icon" src={icon} />
                    </div>

                    <div className="flex flex-row gap-6 justify-around">
                        <div className="flex flex-col">
                            {/* Input File Selection */}
                            <Dropzone
                                id="input-dropzone"
                                height="h-82"
                                width="w-182"
                                label={`Select a ${inputFileType} file`}
                                accept={inputFileType}
                                value={inputFile.file}
                                onChange={handleInputFileChange}
                            />

                            {/* Output Path Selection */}
                            <PathSelector
                                label="Select output path:"
                                value={outputDirPath}
                                placeholder="No folder selected"
                                onSelect={selectOutputDirPath}
                            />
                        </div>
                        <div className="flex flex-col justify-between">
                            <div className="flex flex-col justify-center">
                                {/* Output Files Selection */}
                                {outputFiles.map((outputFile, index) => (
                                    <Dropzone
                                        key={outputFile.fileName}
                                        id={`dropzone-${outputFile.fileName}`}
                                        height="20"
                                        label={`Select ${outputFile.fileName} file`}
                                        accept={outputFileType}
                                        value={outputFile.file}
                                        onChange={handleOutputFileChange(
                                            outputFile.fileName,
                                        )}
                                    />
                                ))}
                            </div>
                            {/* Export Button */}
                            <button
                                type="button"
                                className="bg-blue-500 hover:bg-blue-700 hover:cursor-pointer disabled:bg-gray-400 disabled:cursor-not-allowed text-white font-bold py-2 px-4 rounded mb-1"
                                onClick={handleExportButton}
                                disabled={
                                    !inputFile.filePath ||
                                    (!outputDirPath &&
                                        !outputFiles.every(
                                            (f) => f.file !== null,
                                        )) ||
                                    exportButtonLoading
                                }
                            >
                                {exportButtonLoading
                                    ? 'Exporting...'
                                    : 'Export'}
                            </button>
                        </div>
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
        </div>
    );
}
