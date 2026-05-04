import TextInput from '../components/inputs-fields/TextInput';
import NumberInput from '../components/inputs-fields/NumberInput';
import CategoryInput from '../components/inputs-fields/CategoryInput';
import TextAreaInput from '../components/inputs-fields/TextAreaInput';
import { useEffect, useState } from 'react';
import type { ConfigItem, ConfigValue } from '../../types/config';

interface ConfigModalProps {
    title: string;
    config: ConfigItem[];
    onChange: (id: string, value: ConfigValue) => void;
    onClose: () => void;
    onResetSaved: () => void;
    onResetDefaults: () => void;
    onSubmit: () => void;
    hasUnsavedChanges: boolean;
}

export default function ConfigModal({
    title,
    config,
    onChange,
    onClose,
    onResetSaved,
    onResetDefaults,
    onSubmit,
    hasUnsavedChanges,
}: ConfigModalProps) {
    const [showDiscardWarning, setShowDiscardWarning] = useState(false);

    const handleCancel = () => {
        if (hasUnsavedChanges) {
            setShowDiscardWarning(true);
            return;
        }

        onClose();
    };

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
            {/* Backdrop */}
            <div
                className="absolute inset-0 bg-black/30 backdrop-blur-sm"
                onClick={handleCancel}
            />

            {/* Modal */}
            <div className="relative w-full max-w-lg max-h-[90vh] bg-neutral-primary-soft border border-default rounded-base shadow-sm flex flex-col">
                {/* HEADER */}
                <div className="flex justify-between items-center border-b border-default p-4 shrink-0">
                    <h3 className="text-lg font-medium text-heading">
                        {title}
                    </h3>
                    {hasUnsavedChanges && (
                        <span className="text-xs px-2 py-0.5 rounded-full bg-yellow-500/20 text-yellow-600 border border-yellow-500/30">
                            Unsaved changes
                        </span>
                    )}
                    <button
                        className="hover:cursor-pointer"
                        onClick={handleCancel}
                    >
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
                        disabled={!hasUnsavedChanges}
                        className={`px-4 py-2 rounded-base hover:cursor-pointer ${
                            hasUnsavedChanges
                                ? 'bg-brand text-white'
                                : 'bg-neutral-secondary-medium text-gray-400 cursor-not-allowed'
                        }`}
                    >
                        Save
                    </button>
                    {/* RESET TO SAVED (UNDO) */}
                    <button
                        onClick={onResetSaved}
                        className="bg-neutral-secondary-medium px-4 py-2 rounded-base hover:cursor-pointer"
                    >
                        Undo
                    </button>

                    {/* RESET TO DEFAULT */}
                    <button
                        onClick={onResetDefaults}
                        className="bg-red-500 text-white px-4 py-2 rounded-base hover:cursor-pointer"
                    >
                        Reset to default
                    </button>

                    <button
                        onClick={handleCancel}
                        className="bg-neutral-secondary-medium px-4 py-2 rounded-base hover:cursor-pointer"
                    >
                        Cancel
                    </button>
                </div>
            </div>
            {showDiscardWarning && (
                <div className="absolute inset-0 flex items-center justify-center bg-black/40 backdrop-blur-sm z-50">
                    <div className="bg-white rounded-base p-6 shadow-lg w-90">
                        <h4 className="text-lg font-medium mb-2">
                            Unsaved changes
                        </h4>

                        <p className="text-sm text-gray-600 mb-4">
                            You have unsaved changes. Do you want to discard
                            them?
                        </p>

                        <div className="flex justify-end gap-3">
                            <button
                                onClick={onSubmit}
                                className="px-3 py-2 rounded-base bg-brand text-white"
                            >
                                Save
                            </button>
                            <button
                                onClick={() => setShowDiscardWarning(false)}
                                className="px-3 py-2 rounded-base bg-gray-200"
                            >
                                Keep editing
                            </button>

                            <button
                                onClick={() => {
                                    setShowDiscardWarning(false);
                                    onClose(); // actually close modal
                                }}
                                className="px-3 py-2 rounded-base bg-red-500 text-white"
                            >
                                Discard
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
