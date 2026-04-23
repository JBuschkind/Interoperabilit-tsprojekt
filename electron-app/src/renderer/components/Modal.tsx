import React, { useState } from 'react';

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
        <div className="overflow-y-auto overflow-x-hidden fixed top-0 right-0 left-0 z-50 flex justify-center items-center w-full md:inset-0 h-[calc(100%-1rem)] max-h-full">
            <div className="relative p-4 w-full max-w-md max-h-full">
                <div className="relative bg-neutral-primary-soft border border-default rounded-base shadow-sm p-4 md:p-6">
                    {/* Close Button */}
                    <button
                        onClick={onClose}
                        className="absolute top-3 end-2.5 text-body bg-transparent hover:bg-neutral-tertiary hover:text-heading rounded-base text-sm w-9 h-9 ms-auto inline-flex justify-center items-center"
                    >
                        <svg
                            className="w-5 h-5"
                            xmlns="http://www.w3.org/2000/svg"
                            fill="none"
                            viewBox="0 0 24 24"
                            stroke="currentColor"
                        >
                            <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M6 18L18 6M6 6l12 12"
                            />
                        </svg>
                    </button>

                    {/* Content */}
                    <div className="p-4 md:p-5 text-center">
                        <svg
                            className="mx-auto mb-4 text-fg-disabled w-12 h-12"
                            xmlns="http://www.w3.org/2000/svg"
                            fill="none"
                            viewBox="0 0 24 24"
                            stroke="currentColor"
                        >
                            <path
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                strokeWidth={2}
                                d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                            />
                        </svg>

                        <h3 className="mb-6 text-body">
                            The selected output file already exists. Do you want
                            to merge the changes or overwrite the existing file?
                        </h3>

                        {files.map((file) => (
                            <div
                                key={file.fileName}
                                className="flex justify-between mx-6 my-2"
                            >
                                <label>{file.fileName}: </label>

                                <label className="inline-flex items-center cursor-pointer">
                                    <span className="text-sm font-medium text-heading">
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

                                    <div className="relative mx-3 w-9 h-5 bg-neutral-quaternary peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-brand-soft rounded-full peer peer-checked:after:translate-x-full peer-checked:bg-brand after:content-[''] after:absolute after:top-[2px] after:start-[2px] after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all"></div>

                                    <span className="text-sm font-medium text-heading">
                                        Merge
                                    </span>
                                </label>
                            </div>
                        ))}
                        <div className="flex items-center justify-around mt-6">
                            <button
                                onClick={onAccept}
                                className="text-white bg-blue-500 hover:bg-blue-700 hover:cursor-pointer box-border border border-transparent shadow-xs font-medium leading-5 rounded-base text-sm px-4 py-2.5 focus:outline-none"
                            >
                                {acceptButtonLoading
                                    ? 'Generating Code...'
                                    : 'Accept'}
                            </button>

                            <button
                                onClick={onClose}
                                className="text-body bg-neutral-secondary-medium hover:cursor-pointer box-border border border-default-medium hover:bg-neutral-tertiary-medium hover:text-heading focus:ring-4 focus:ring-neutral-tertiary shadow-xs font-medium leading-5 rounded-base text-sm px-4 py-2.5 focus:outline-none"
                            >
                                No, cancel
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
