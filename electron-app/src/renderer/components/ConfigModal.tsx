import TextInput from '../components/inputs-fields/TextInput';
import NumberInput from '../components/inputs-fields/NumberInput';
import CategoryInput from '../components/inputs-fields/CategoryInput';
import TextAreaInput from '../components/inputs-fields/TextAreaInput';
import { useEffect, useState } from 'react';
import type { ConfigItem, ConfigValue } from '../../types/config';
import { Button } from './Button';

interface ConfigModalProps {
    title: string;
    config: ConfigItem[];
    onChange: (id: string, value: ConfigValue) => void;
    onClose: () => void;
    onResetSaved: () => void;
    onResetDefaults: () => void;
    onSubmit: () => void;
    closeModal: () => void;
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
    closeModal,
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
            <div className="flex flex-col relative w-full max-w-2xl max-h-[90vh] bg-surface-container-low  rounded-xs shadow-sm  text-surface-inverse">
                {/* HEADER */}
                <div className="flex justify-between items-center  p-4 shrink-0">
                    <div className="w-34  flex justify-center items-center gap-2 text-primary text-2xl">
                        <span className="material-symbols-outlined ">tune</span>

                        <h3 className="font-medium ">{title}</h3>
                    </div>

                    {hasUnsavedChanges && (
                        <span className="text-xs px-2 py-0.5 rounded-full bg-yellow-500/20 text-yellow-400 border border-yellow-500/30">
                            Unsaved changes
                        </span>
                    )}

                    <div className="w-34 flex items-center justify-end">
                        <button
                            className="flex items-center justify-center hover:cursor-pointer rounded-full p-1 text-center hover:bg-surface-bright"
                            onClick={handleCancel}
                        >
                            <span className="material-symbols-outlined">
                                Close
                            </span>
                        </button>
                    </div>
                </div>

                {/* BODY */}
                <div className="flex-1 overflow-y-auto scrollbar-custom p-6 flex flex-col gap-4">
                    {config.map(renderField)}
                </div>

                {/* FOOTER */}
                <div className="flex gap-4 p-4 shrink-0">
                    <Button
                        variant="primary"
                        className="flex-1"
                        onClick={onSubmit}
                        disabled={!hasUnsavedChanges}
                    >
                        Save
                    </Button>
                    {/* RESET TO SAVED (UNDO) */}
                    <Button onClick={onResetSaved}>Undo</Button>

                    {/* RESET TO DEFAULT */}
                    <Button onClick={onResetDefaults}>Reset to default</Button>

                    <Button onClick={handleCancel}>Cancel</Button>
                </div>
            </div>
            {showDiscardWarning && (
                <div className=" absolute inset-0 flex items-center justify-center bg-black/50 backdrop-blur-sm z-50 text-surface-inverse">
                    <div className="bg-surface-container-high rounded-base p-6 shadow-lg w-120">
                        <h4 className="text-primary text-lg font-medium mb-2">
                            Unsaved changes
                        </h4>

                        <p className="text-sm mb-4">
                            You have unsaved changes. Do you want to discard
                            them?
                        </p>

                        <div className="flex justify-end gap-3">
                            <Button
                                variant="primary"
                                onClick={() => {
                                    onSubmit();
                                    closeModal();
                                }}
                                disabled={!hasUnsavedChanges}
                            >
                                Save
                            </Button>
                            <Button
                                onClick={() => setShowDiscardWarning(false)}
                            >
                                Keep editing
                            </Button>

                            <Button
                                variant="danger"
                                onClick={() => {
                                    setShowDiscardWarning(false);
                                    onClose(); // actually close modal
                                }}
                            >
                                Discard
                            </Button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
