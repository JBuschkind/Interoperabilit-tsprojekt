type TextInputProps = {
    id: string;
    label: string;
    value?: string;
    onChange?: (value: string) => void;
    placeholder?: string;
    error?: string;
    type?: string;
    name?: string;
    disabled?: boolean;
};

export default function TextInput({
    id,
    label,
    value = '',
    onChange,
    placeholder = '',
    error,
    type = 'text',
    name,
    disabled = false,
}: TextInputProps) {
    return (
        <div>
            <label
                htmlFor={id}
                className="block mb-2.5 text-sm font-medium text-heading"
            >
                {label}
            </label>

            <input
                id={id}
                name={name}
                type={type}
                value={value}
                onChange={(e) => onChange?.(e.target.value)}
                placeholder={placeholder}
                disabled={disabled}
                className={`bg-neutral-secondary-medium border border-default-medium text-heading text-sm rounded-base focus:ring-brand focus:border-brand block w-full px-3 py-2.5 shadow-xs placeholder:text-body ${
                    error
                        ? 'border-red-500 focus:border-red-500 focus:ring-red-500'
                        : ''
                }`}
            />

            {error && <p className="mt-1 text-sm text-red-500">{error}</p>}
        </div>
    );
}
