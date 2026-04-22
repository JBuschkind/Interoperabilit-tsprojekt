import React from 'react';

type PathSelectorProps = {
    label: string;
    value: string | null | undefined;
    placeholder: string;
    onSelect: () => void;
    browseText?: string;
};

export const PathSelector: React.FC<PathSelectorProps> = ({
    label,
    value,
    placeholder,
    onSelect,
    browseText = 'Browse',
}) => {
    return (
        <div>
            <label className="block mb-2 text-sm font-medium text-heading">
                {label}
            </label>

            <button
                type="button"
                onClick={onSelect}
                className="flex items-center justify-between w-full px-4 py-3 text-sm text-left bg-neutral-secondary-medium border border-default-medium rounded-base shadow-xs hover:bg-neutral-tertiary-medium transition cursor-pointer"
            >
                <span className={value ? 'text-heading' : 'text-body'}>
                    {value ?? placeholder}
                </span>

                <span className="text-xs text-body">{browseText}</span>
            </button>
        </div>
    );
};
