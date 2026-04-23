import React, { useEffect, useRef, useState } from 'react';
import icon from '../../../assets/icon.svg';
import Dropzone from './Dropzone';
import { PathSelector } from './PathSelector';
import Modal from './Modal';
import { Merger } from './Merger';
import { log } from 'console';

type CodeGeneratorProps = {
    inputFileType?: string;
    parameter?: string[];
    outputFileNames?: string[];
    callCLI: (args: string[]) => Promise<string>;
};

export default function CodeGenerator({
    inputFileType = '.db',
    parameter = ['ClassName', 'Variable A'],
    outputFileNames = ['SPS', 'SPSProxy'],
    callCLI,
}: CodeGeneratorProps) {
    type InputFile = {
        fileName: string | null;
        file: File | null;
        filePath: string | null;
    };

    type OutputFile = {
        fileName: string;
        file: File | null;
        originalCode: string | null;
        mergedCode: string | null;
        generatedCode: string | null;
        filePath: string | null;
        tempFilePath: string | null;
        toBeMerged: boolean;
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
            originalCode: null,
            mergedCode: null,
            generatedCode: null,
            filePath: null,
            tempFilePath: null,
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

    // Sets filePath for output files that don't have a file selected
    const resolveDefaultOutputFilePaths = async (
        files: OutputFile[],
        outputDirPath: string,
    ) => {
        return Promise.all(
            files.map(async (outputFile) => {
                if (outputFile.file) {
                    return outputFile;
                }
                const filePath = await window.electron.ipcRenderer.joinPath(
                    outputDirPath,
                    `${outputFile.fileName}.cs`,
                );
                return {
                    ...outputFile,
                    filePath,
                };
            }),
        );
    };

    const handleExportButton = async () => {
        if (!inputFile.filePath) return; // TODO: Show Input field feedback
        if (!outputDirPath) return; // TODO: Show Output path feedback 2. TODO: This needs to consider the case all merge files are selected

        // Set filePath for output files that don't have a file selected
        const updatedOutputFiles = await resolveDefaultOutputFilePaths(
            outputFiles,
            outputDirPath,
        );

        setOutputFiles(updatedOutputFiles);

        // If any already exiting output files are selected, open the modal
        if (outputFiles.some((outputFile) => outputFile.file !== null)) {
            setUiState(UIState.DecideMerge);
            return;
        }

        // Extract filePaths to pass to CLI
        const outputPaths = updatedOutputFiles.map((file) => file.filePath);

        // Call CLI with input file path and all output paths
        await callCLI([inputFile.filePath, ...outputPaths]);

        clearState();
        setUiState(UIState.Idle);
    };

    // Sets all output paths and temp output paths
    const resolveOutputFilePaths = async (outputFiles: OutputFile[]) => {
        return Promise.all(
            outputFiles.map(async (outputFile) => {
                // Keep file paths for not selected output files
                if (!outputFile.file) {
                    return {
                        ...outputFile,
                    };
                }

                const filePath = await window.electronApi.getFilePath(
                    outputFile.file,
                );

                const parsed =
                    await window.electron.ipcRenderer.parseFilePath(filePath);

                const tempFilePath = await window.electron.ipcRenderer.joinPath(
                    parsed.dir,
                    `${parsed.name}.temp${parsed.ext}`,
                );

                return {
                    ...outputFile,
                    filePath,
                    tempFilePath,
                };
            }),
        );
    };

    const buildMergeQueue = (files: OutputFile[]): OutputFile[] => {
        return files.filter((file) => file.toBeMerged && file.file !== null);
    };

    const handleAcceptModal = async () => {
        if (!inputFile.filePath) return; // TODO: Handle Cases

        // 1. Get all output paths and all temp paths
        const updatedFiles = await resolveOutputFilePaths(outputFiles);

        // 2. Read in source code for files that will be merged
        updatedFiles.forEach(async (file) => {
            if (file.toBeMerged && file.filePath && file.file) {
                //TODO: Problem is that we handle file.filePath before as an "output path" but it doesnt exit if file.file is not set
                // TODO: Differentiate between filePath and outputPath
                // Read in File
                file.originalCode = await window.electron.ipcRenderer.readFile(
                    file.filePath,
                );
            }
        });

        setOutputFiles(updatedFiles);

        // Build output paths: tempFilePath for files to be merged, filePath for others
        const outputPaths = updatedFiles.map((file) =>
            file.toBeMerged && file.tempFilePath
                ? file.tempFilePath
                : file.filePath,
        );

        // Call CLI with input file path and all output paths
        await callCLI([inputFile.filePath, ...outputPaths]);

        // Read generated code from temp files for files that need to be merged
        const filesWithGeneratedCode = await Promise.all(
            updatedFiles.map(async (file) => {
                if (file.toBeMerged && file.tempFilePath) {
                    const generatedCode =
                        await window.electron.ipcRenderer.readFile(
                            file.tempFilePath,
                        );
                    return { ...file, generatedCode };
                }
                return file;
            }),
        );

        setOutputFiles(filesWithGeneratedCode);

        // 3. Build queue ONLY from selected files
        const queue = buildMergeQueue(filesWithGeneratedCode);
        setMergeQueue(queue);

        if (queue.length === 0) {
            clearState();
            setUiState(UIState.Idle);
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
        if (!currentTask?.filePath || !currentTask.tempFilePath) return;

        await window.electron.ipcRenderer.finalizeMerge({
            outputPath: currentTask.filePath,
            mergedCode: mergedCode,
        });

        window.electron.ipcRenderer.deleteTempFile(currentTask.tempFilePath);

        goToNextTask();
    };

    const handleCancelMerge = () => {
        return;
        // window.electron.ipcRenderer.deleteTempFile(outputPath + '.temp.cs');
        // setMergeQueue([]);
        // setCurrentTask(null);
        // setUiState(UIState.Idle);
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
                originalCode: null,
                mergedCode: null,
                generatedCode: null,
                filePath: null,
                tempFilePath: null,
                toBeMerged: true,
            })),
        );
    };

    return (
        <div className="flex-1 px-6 py-8">
            {/* Main Content */}
            {(uiState === UIState.Idle || uiState === UIState.DecideMerge) && (
                <form className="max-w-4xl mx-auto flex flex-col gap-4 bg-gray-300 p-8 rounded-lg shadow-md">
                    <div className="flex flex-col items-center justify-center ">
                        <img width="300" alt="icon" src={icon} />
                    </div>

                    <div className="flex flex-row justify-around">
                        <div className="flex flex-col">
                            {/* Input File Selection */}
                            <Dropzone
                                id="input-dropzone"
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

                        <div className="flex flex-col">
                            {/* Output Files Selection */}
                            {outputFiles.map((outputFile, index) => (
                                <Dropzone
                                    key={outputFile.fileName}
                                    id={`dropzone-${outputFile.fileName}`}
                                    label={`Select ${outputFile.fileName} file`}
                                    accept=".cs"
                                    value={outputFile.file}
                                    onChange={handleOutputFileChange(
                                        outputFile.fileName,
                                    )}
                                />
                            ))}
                        </div>
                    </div>

                    {/* Export Button */}
                    <button
                        type="button"
                        className="bg-blue-500 hover:bg-blue-700 hover:cursor-pointer disabled:bg-gray-400 disabled:cursor-not-allowed text-white font-bold py-2 px-4 rounded"
                        onClick={handleExportButton}
                        disabled={
                            !inputFile.filePath ||
                            (!outputDirPath &&
                                !outputFiles.every((f) => f.file !== null)) ||
                            exportButtonLoading
                        }
                    >
                        {exportButtonLoading ? 'Exporting...' : 'Export'}
                    </button>
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
                    originalCode={currentTask.originalCode}
                    modifiedCode={currentTask.generatedCode}
                    onAcceptMerge={handleAcceptMerge}
                    onCancelMerge={handleCancelMerge}
                />
            )}
        </div>
    );
}
