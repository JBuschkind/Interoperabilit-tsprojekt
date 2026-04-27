import React from 'react';

type PathSelectorProps = {
    value: string | null | undefined;
    placeholder: string;
    onSelect: () => void;
    browseText?: string;
};

export const PathSelector: React.FC<PathSelectorProps> = ({
    value,
    placeholder,
    onSelect,
    browseText = 'Browse',
}) => {
    return (
        <>
            <div>
                <button
                    type="button"
                    onClick={onSelect}
                    className="w-full flex items-center justify-between bg-surface-container-lowest border-0 font-mono text-[11px] text-sm text-left  shadow-xs  transition cursor-pointer"
                >
                    <span
                        className={
                            value
                                ? 'text-surface-inverse px-3'
                                : 'text-surface-inverse/60  px-3'
                        }
                    >
                        {value ?? placeholder}
                    </span>

                    <div className="min-h-10 flex justify-center items-center px-2 bg-surface-container-high hover:bg-surface-container-highest">
                        <span className="material-symbols-outlined text-surface-inverse/60">
                            folder
                        </span>
                    </div>
                </button>
            </div>
        </>
    );
};
