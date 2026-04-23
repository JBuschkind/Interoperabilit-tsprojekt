import React, { useEffect, useRef, useState } from 'react';

type DropzoneProps = {
    id?: string;
    label?: string;
    accept?: string;
    value?: File | null;
    onChange?: (file: File | null) => void;
    maxSizeMB?: number;
    className?: string;
    width?: string;
    height?: string;
};

export default function Dropzone({
    id = 'dropzone',
    label = 'File',
    accept = '*',
    value = null,
    onChange,
    maxSizeMB,
    className = '',
    width = 'w-92',
    height = 'h-44',
}: DropzoneProps) {
    const [file, setFile] = useState<File | null>(value);
    const [isDragging, setIsDragging] = useState(false);

    const inputRef = useRef<HTMLInputElement | null>(null);

    useEffect(() => {
        setFile(value);
    }, [value]);

    const formatSize = (size?: number) => {
        if (!size) return '';
        return `${(size / 1024).toFixed(1)} KB`;
    };

    const validateFile = (f: File): boolean => {
        if (maxSizeMB && f.size > maxSizeMB * 1024 * 1024) {
            alert(`File exceeds ${maxSizeMB}MB limit`);
            return false;
        }
        return true;
    };

    const handleFile = (f: File | null) => {
        if (!f) return;

        if (!validateFile(f)) return;

        console.log(f);

        setFile(f);
        onChange?.(f);

        // allows selecting the same file again after removing it
        if (inputRef.current) {
            inputRef.current.value = '';
        }
    };

    const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        setIsDragging(false);

        const droppedFile = e.dataTransfer.files?.[0];
        handleFile(droppedFile);
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const selected = e.target.files?.[0] ?? null;
        handleFile(selected);
    };

    const clearFile = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.preventDefault();

        setFile(null);
        onChange?.(null);

        if (inputRef.current) {
            inputRef.current.value = '';
        }
    };

    return (
        <div className={`mb-5 ${className}`}>
            <label
                htmlFor={`${id}-input`}
                className="block mb-2.5 text-sm font-medium text-heading"
            >
                {label}
            </label>

            <div
                onDragOver={(e) => {
                    e.preventDefault();
                    setIsDragging(true);
                }}
                onDragLeave={() => setIsDragging(false)}
                onDrop={handleDrop}
                className={`flex items-center justify-center border border-dashed rounded-base transition-all cursor-pointer ${width} ${height}
                ${
                    isDragging
                        ? 'bg-neutral-tertiary-medium border-primary'
                        : 'bg-neutral-secondary-medium border-default-strong'
                }
                ${file ? 'bg-green-50 border-green-400' : ''}`}
            >
                <label
                    htmlFor={`${id}-input`}
                    className="w-full h-full flex items-center justify-center cursor-pointer"
                >
                    <input
                        ref={inputRef}
                        id={`${id}-input`}
                        type="file"
                        accept={accept}
                        className="hidden"
                        onChange={handleChange}
                    />

                    {!file ? (
                        <div className="flex flex-col items-center justify-center text-body pt-5 pb-6">
                            <svg
                                className="w-8 h-8 mb-4"
                                xmlns="http://www.w3.org/2000/svg"
                                fill="none"
                                viewBox="0 0 24 24"
                                stroke="currentColor"
                            >
                                <path
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    strokeWidth={2}
                                    d="M15 17h3a3 3 0 0 0 0-6h-.025a5.56 5.56 0 0 0 .025-.5A5.5 5.5 0 0 0 7.207 9.021C7.137 9.017 7.071 9 7 9a4 4 0 1 0 0 8h2.167M12 19v-9m0 0-2 2m2-2 2 2"
                                />
                            </svg>

                            <p className="mb-2 text-sm">
                                <span className="font-semibold">
                                    Click to upload
                                </span>{' '}
                                or drag and drop
                            </p>
                            <p className="text-xs">
                                {accept === '*' ? 'Any file' : accept}
                            </p>
                        </div>
                    ) : (
                        <div className="flex flex-col items-center justify-center text-center">
                            <div className="w-12 h-12 mb-3 rounded-full bg-green-100 flex items-center justify-center">
                                <svg
                                    className="w-6 h-6 text-green-600"
                                    fill="none"
                                    stroke="currentColor"
                                    viewBox="0 0 24 24"
                                >
                                    <path
                                        strokeLinecap="round"
                                        strokeLinejoin="round"
                                        strokeWidth={2}
                                        d="M5 13l4 4L19 7"
                                    />
                                </svg>
                            </div>

                            <p className="text-sm font-medium text-heading">
                                File ready
                            </p>

                            <p className="text-xs text-body mt-1">
                                {file.name}
                            </p>

                            <p className="text-xs text-body">
                                {formatSize(file.size)}
                            </p>

                            <button
                                className="mt-3 text-xs text-red-500 hover:underline"
                                onClick={clearFile}
                            >
                                Remove file
                            </button>
                        </div>
                    )}
                </label>
            </div>
        </div>
    );
}
