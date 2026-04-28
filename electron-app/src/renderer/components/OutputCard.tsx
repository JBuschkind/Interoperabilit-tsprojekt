type OutputCardProps = {
    selected: boolean;
    disabled: boolean;
    onSelect: () => void;
    icon: string;
    title: string;
    description: string;
    name: string;
    checked: boolean;
    children: React.ReactNode;
};

export function OutputCard({
    selected,
    disabled = false,
    onSelect,
    icon,
    title,
    description,
    name,
    checked,
    children,
}: OutputCardProps) {
    const handleClick = () => {
        if (!disabled) onSelect();
    };

    return (
        <div
            onClick={handleClick}
            className={`flex flex-col w-1/2 min-w-0 bg-surface-container-low p-5 border-l-2 transition-all
                ${
                    disabled
                        ? 'opacity-30 grayscale cursor-not-allowed '
                        : 'cursor-pointer'
                }
                ${
                    !disabled && selected
                        ? 'border-primary-container opacity-100'
                        : 'border-transparent'
                }
                ${
                    !disabled && !selected
                        ? 'hover:border-primary-container opacity-40 hover:opacity-60 '
                        : ''
                }
            `}
        >
            {/* Block interaction completely when disabled */}
            {disabled && (
                <div className="absolute inset-0 z-10 cursor-not-allowed" />
            )}

            {/* Header */}
            <div className="flex items-start justify-between mb-4">
                <div className="flex items-center gap-3">
                    <div className="w-10 h-10 bg-primary-container/10 flex items-center justify-center rounded">
                        <span className="material-symbols-outlined text-primary">
                            {icon}
                        </span>
                    </div>
                    <div>
                        <h3 className="text-sm font-bold uppercase tracking-tight text-primary">
                            {title}
                        </h3>
                        <p className="text-xs text-surface-inverse">
                            {description}
                        </p>
                    </div>
                </div>

                <input
                    type="radio"
                    name={name}
                    checked={checked}
                    disabled={disabled}
                    onChange={(e) => {
                        e.stopPropagation();
                        if (!disabled) onSelect();
                    }}
                />
            </div>

            {children}
        </div>
    );
}
