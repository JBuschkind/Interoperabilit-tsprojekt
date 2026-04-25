type TextAreaInputProps = {
    id: string;
    label: string;
    value: string;
    onChange: (value: string) => void;
    placeholder?: string;
    error?: string;
    name?: string;
    disabled?: boolean;
    rows?: number;
};

export default function TextAreaInput({
    id,
    label,
    value,
    onChange,
    placeholder = '',
    error,
    name,
    disabled = false,
    rows = 4,
}: TextAreaInputProps) {
    return (
        <div>
            <label
                htmlFor={id}
                className="block mb-2.5 text-sm font-medium text-heading"
            >
                {label}
            </label>

            <textarea
                id={id}
                name={name}
                value={value}
                rows={rows}
                placeholder={placeholder}
                disabled={disabled}
                onChange={(e) => onChange(e.target.value)}
                className={`block bg-neutral-secondary-medium border border-default-medium text-heading text-sm rounded-base focus:ring-brand focus:border-brand w-full p-3.5 shadow-xs placeholder:text-body ${
                    error
                        ? 'border-red-500 focus:border-red-500 focus:ring-red-500'
                        : ''
                }`}
            />

            {error && <p className="mt-1 text-sm text-red-500">{error}</p>}
        </div>
    );
}
