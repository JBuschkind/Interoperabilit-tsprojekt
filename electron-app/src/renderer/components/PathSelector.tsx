import React from 'react';

type PathSelectorProps = {
    value: string | null | undefined;
    placeholder: string;
    onSelect: () => void;
    browseText?: string;
    error?: boolean;
};

export const PathSelector: React.FC<PathSelectorProps> = ({
    value,
    placeholder,
    onSelect,
    browseText = 'Browse',
    error = false,
}) => {
    return (
        <>
            <div>
                <button
                    type="button"
                    onClick={onSelect}
                    className={`w-full flex items-center justify-between font-mono text-[11px] text-sm text-left shadow-xs transition cursor-pointer ${
                        error
                            ? 'border-2 border-red-500 bg-surface-container-lower'
                            : 'bg-surface-container-lower border-0'
                    }`}
                >
                    <span
                        className={
                            value
                                ? 'text-textcolor px-3'
                                : 'text-textcolor/60  px-3'
                        }
                    >
                        {value ?? placeholder}
                    </span>

                    <div className="min-h-10 flex justify-center items-center px-2 bg-surface-container-high hover:bg-surface-container-highest">
                        <span className="material-symbols-outlined text-textcolor/60">
                            folder
                        </span>
                    </div>
                </button>
            </div>
        </>
    );
};
