import React, { useState } from 'react';
import { Button } from './Button';

interface ModalProps {
    acceptButtonLoading: boolean;
    onClose: () => void;
    onAccept: () => void;

    files: {
        fileName: string;
        toBeMerged: boolean;
    }[];

    onToggleChange: (fileName: string, value: boolean) => void;
}

export default function Modal({
    acceptButtonLoading,
    onClose,
    onAccept,
    files,
    onToggleChange,
}: ModalProps) {
    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center text-textcolor">
            {/* Backdrop */}
            <div
                className="absolute inset-0 bg-black/30 backdrop-blur-sm"
                onClick={onClose}
            />

            <div className="relative bg-surface-container-low  max-w-lg rounded-xs shadow-lg p-4 md:p-6">
                {/* Close Button */}
                <button
                    className="absolute top-3 inset-e-3 flex items-center justify-center hover:cursor-pointer rounded-full p-1 text-center hover:bg-surface-container-highest"
                    onClick={onClose}
                >
                    <span className="material-symbols-outlined">Close</span>
                </button>

                {/* Content */}
                <div className="flex flex-col p-4 md:p-5 text-center gap-2">
                    <span className=" material-symbols-outlined text-5xl! mb-2 text-yellow-500">
                        Error
                    </span>

                    <h3 className="mb-6">
                        The selected output file already exists. Do you want to
                        merge the changes or overwrite the existing file?
                    </h3>

                    {files.map((file) => (
                        <div
                            key={file.fileName}
                            className="flex justify-between mx-6 my-2 "
                        >
                            <label className="font-medium ">
                                {file.fileName}:
                            </label>

                            <label className="inline-flex items-center cursor-pointer">
                                <span className="text-sm font-medium ">
                                    Override
                                </span>

                                <input
                                    type="checkbox"
                                    className="sr-only peer"
                                    checked={file.toBeMerged}
                                    onChange={(e) =>
                                        onToggleChange(
                                            file.fileName,
                                            e.target.checked,
                                        )
                                    }
                                />

                                <div className="relative mx-3 w-9 h-5 bg-surface-container-highest peer-focus:outline-none  rounded-full peer peer-checked:after:translate-x-full peer-checked:bg-primary-container/60 after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all"></div>

                                <span className="text-sm font-medium ">
                                    Merge
                                </span>
                            </label>
                        </div>
                    ))}
                    <div className="flex items-center justify-around mt-6">
                        <Button variant="primary" onClick={onAccept}>
                            Accept
                        </Button>
                        <Button onClick={onClose}>No, cancel</Button>
                    </div>
                </div>
            </div>
        </div>
    );
}
