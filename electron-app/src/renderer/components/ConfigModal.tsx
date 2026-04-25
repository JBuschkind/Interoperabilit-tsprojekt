import TextInput from '../components/inputs-fields/TextInput';
import NumberInput from '../components/inputs-fields/NumberInput';
import CategoryInput from '../components/inputs-fields/CategoryInput';
import TextAreaInput from '../components/inputs-fields/TextAreaInput';

import type { ConfigItem, ConfigValue } from '../../types/config';

interface ConfigModalProps {
    title: string;
    config: ConfigItem[];
    onChange: (id: string, value: ConfigValue) => void;
    onClose: () => void;
    onSubmit: () => void;
}

export default function ConfigModal({
    title,
    config,
    onChange,
    onClose,
    onSubmit,
}: ConfigModalProps) {
    const renderField = (item: ConfigItem) => {
        switch (item.type) {
            case 'text':
                return (
                    <TextInput
                        key={item.id}
                        id={item.id}
                        label={item.label}
                        value={item.value as string}
                        onChange={(v) => onChange(item.id, v)}
                        placeholder={item.placeholder}
                    />
                );

            case 'number':
                return (
                    <NumberInput
                        key={item.id}
                        id={item.id}
                        label={item.label}
                        value={item.value as number | ''}
                        onChange={(v) => onChange(item.id, v)}
                        placeholder={item.placeholder}
                    />
                );

            case 'select':
                return (
                    <CategoryInput
                        key={item.id}
                        id={item.id}
                        label={item.label}
                        value={(item.value as string) ?? ''}
                        onChange={(v) => onChange(item.id, v)}
                        options={item.options}
                    />
                );

            case 'textarea':
                return (
                    <TextAreaInput
                        key={item.id}
                        id={item.id}
                        label={item.label}
                        value={item.value as string}
                        onChange={(v) => onChange(item.id, v)}
                        placeholder={item.placeholder}
                        rows={item.rows}
                    />
                );
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
            <div className="w-full max-w-lg max-h-[90vh] bg-neutral-primary-soft border border-default rounded-base shadow-sm flex flex-col">
                {/* HEADER */}
                <div className="flex justify-between items-center border-b border-default p-4 shrink-0">
                    <h3 className="text-lg font-medium text-heading">
                        {title}
                    </h3>
                    <button className="hover:cursor-pointer" onClick={onClose}>
                        ✕
                    </button>
                </div>

                {/* BODY */}
                <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-4">
                    {config.map(renderField)}
                </div>

                {/* FOOTER */}
                <div className="flex gap-4 border-t border-default p-4 shrink-0">
                    <button
                        onClick={onSubmit}
                        className="bg-brand text-white px-4 py-2 rounded-base hover:cursor-pointer"
                    >
                        Submit
                    </button>

                    <button
                        onClick={onClose}
                        className="bg-neutral-secondary-medium px-4 py-2 rounded-base hover:cursor-pointer"
                    >
                        Cancel
                    </button>
                </div>
            </div>
        </div>
    );
}
