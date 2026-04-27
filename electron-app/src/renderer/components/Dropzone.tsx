import { useDropzone } from '../hooks/useDropzone';

type DropzoneProps = {
    id?: string;
    accept?: string;
    value?: File | null;
    onChange?: (file: File | null) => void;
    maxSizeMB?: number;
    className?: string;
    smallMode?: boolean;
};

export default function Dropzone({
    id = 'dropzone',
    accept = '*',
    value = null,
    onChange,
    maxSizeMB,
}: DropzoneProps) {
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

    return (
        <label
            htmlFor={`${id}-input`}
            onDragOver={(e) => {
                e.preventDefault();
                setIsDragging(true);
            }}
            onDragLeave={() => setIsDragging(false)}
            onDrop={handleDrop}
            className={`flex flex-col items-center justify-center border-2 border-dashed transition-all cursor-pointer h-54 w-full
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
                // No file Placeholder
                <div className="flex flex-col items-center justify-center text-body pt-5 pb-6 gap-4">
                    <div className="w-16 h-16 bg-surface-container flex items-center justify-center rounded-sm text-surface-inverse/60 group-hover:text-primary transition-colors">
                        <span className="material-symbols-outlined text-4xl">
                            upload_file
                        </span>
                    </div>
                    <p className="font-headline font-bold text-lg text-surface-inverse uppercase tracking-tight">
                        Drag &amp; Drop {accept === '*' ? 'ANY ' : accept} File
                    </p>
                </div>
            ) : (
                // File Info + File Remove Button
                <div className="flex flex-col items-center justify-center text-center">
                    <div className="w-12 h-12 mb-3 rounded-full bg-green-300 flex items-center justify-center">
                        <svg
                            className="w-6 h-6 text-success"
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

                    <p className="text-sm font-medium text-surface-inverse">
                        File ready
                    </p>

                    <p className="text-xs text-surface-inverse/60 mt-1">
                        {file.name} ({formatSize(file.size)})
                    </p>

                    <button
                        className="mt-3 text-xs text-red-400 hover:underline cursor-pointer"
                        onClick={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            clearFile();
                        }}
                    >
                        Remove file
                    </button>
                </div>
            )}
        </label>
    );
}
