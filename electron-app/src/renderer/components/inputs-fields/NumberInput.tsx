type NumberInputProps = {
    id: string;
    label: string;
    value: number | '';
    onChange: (value: number | '') => void;
    placeholder?: string;
    error?: string;
    name?: string;
    disabled?: boolean;
    min?: number;
    max?: number;
    step?: number;
};

export default function NumberInput({
    id,
    label,
    value,
    onChange,
    placeholder = '',
    error,
    name,
    disabled = false,
    min,
    max,
    step,
}: NumberInputProps) {
    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const val = e.target.value;

        // Allow empty input
        if (val === '') {
            onChange('');
            return;
        }

        onChange(Number(val));
    };

    return (
        <div>
            <label
                htmlFor={id}
                className="block mb-2.5 text-sm font-medium text-heading"
            >
                {label}
            </label>

            <input
                type="number"
                id={id}
                name={name}
                value={value}
                onChange={handleChange}
                placeholder={placeholder}
                disabled={disabled}
                min={min}
                max={max}
                step={step}
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
