import { useEffect, useState } from 'react';
import { useDropzone } from '../hooks/useDropzone';

type MiniDropzoneProps = {
    id?: string;
    accept?: string;
    value?: File | null;
    onChange?: (file: File | null) => void;
    maxSizeMB?: number;
    className?: string;
    smallMode?: boolean;
};

export default function MiniDropzone({
    id = 'MiniDropzone',
    accept = '*',
    value = null,
    onChange,
    maxSizeMB,
}: MiniDropzoneProps) {
    const {
        file,
        isDragging,
        inputRef,
        formatSize,
        setIsDragging,
        handleDrop,
        handleChange,
        clearFile,
    } = useDropzone({ value, onChange, maxSizeMB });

    const [filePath, setFilePath] = useState<string | null>(null);

    useEffect(() => {
        const fetchPath = async () => {
            if (file) {
                const path = await window.electronApi.getFilePath(file);
                setFilePath(path);
            } else {
                setFilePath(null);
            }
        };
        fetchPath();
    }, [file]);

    return (
        <label
            htmlFor={`${id}-input`}
            onDragOver={(e) => {
                e.preventDefault();
                setIsDragging(true);
            }}
            onDragLeave={() => setIsDragging(false)}
            onDrop={handleDrop}
            className={`flex items-center justify-center border-2 border-dashed transition-all cursor-pointer w-full h-17
                ${
                    isDragging
                        ? 'bg-surface-container border-primary/50 '
                        : 'bg-surface-container-lowest'
                }
                ${file ? 'bg-green-50 border-green-300' : 'border-outline/30'}`}
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
                <p className="font-headline font-bold text-sm text-surface-inverse uppercase tracking-tight">
                    Drag &amp; Drop {accept === '*' ? 'ANY ' : accept} File
                </p>
            ) : (
                <div className="flex min-w-0 items-center gap-3 bg-surface-container-lowest p-3  w-full">
                    <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between mb-1">
                            <span className="mono-technical text-2xs text-primary uppercase font-bold tracking-widest">
                                {file.name}
                            </span>
                        </div>
                        <div
                            className="truncate text-[11px] text-surface-inverse mono-technical"
                            title={filePath ?? undefined}
                        >
                            {filePath}
                        </div>
                    </div>
                    <button
                        onClick={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            clearFile();
                        }}
                        className="flex justify-center items-center text-surface-inverse p-1 hover:text-error cursor-pointer"
                    >
                        <span className="material-symbols-outlined text-lg">
                            close
                        </span>
                    </button>
                </div>
            )}
        </label>
    );
}
