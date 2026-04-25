type Option = {
    label: string;
    value: string;
};

type CategoryInputProps = {
    id: string;
    label: string;
    value: string;
    onChange: (value: string) => void;
    options: Option[];
    placeholder?: string;
    error?: string;
    name?: string;
    disabled?: boolean;
};

export default function CategoryInput({
    id,
    label,
    value,
    onChange,
    options,
    placeholder = 'Select an option',
    error,
    name,
    disabled = false,
}: CategoryInputProps) {
    return (
        <div>
            <label
                htmlFor={id}
                className="block mb-2.5 text-sm font-medium text-heading"
            >
                {label}
            </label>

            <select
                id={id}
                name={name}
                value={value}
                onChange={(e) => onChange(e.target.value)}
                disabled={disabled}
                className={`block w-full bg-neutral-secondary-medium border border-default-medium text-heading text-sm rounded-base focus:ring-brand focus:border-brand px-3 py-2.5 shadow-xs placeholder:text-body ${
                    error
                        ? 'border-red-500 focus:border-red-500 focus:ring-red-500'
                        : ''
                }`}
            >
                <option value="" disabled>
                    {placeholder}
                </option>

                {options.map((option) => (
                    <option key={option.value} value={option.value}>
                        {option.label}
                    </option>
                ))}
            </select>

            {error && <p className="mt-1 text-sm text-red-500">{error}</p>}
        </div>
    );
}
