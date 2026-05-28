import { Button } from './Button';

type OverwritePreviewProps = {
    fileName: string;
    generatedCode: string;
    onApprove: () => void;
    onCancel: () => void;
    loading?: boolean;
};

export const OverwritePreview: React.FC<OverwritePreviewProps> = ({
    fileName,
    generatedCode,
    onApprove,
    onCancel,
    loading = false,
}) => {
    return (
        <div className="flex flex-col flex-1 w-full min-h-0 px-6 py-4">
            <div className="text-textcolor flex justify-center mb-3 gap-2 flex-shrink-0">
                <span className="font-bold">File:</span>
                <span>{fileName}</span>
            </div>

            <div className="border border-gray-300 rounded-md w-full h-8 flex items-center justify-center flex-shrink-0">
                <div className="text-textcolor text-center font-medium">
                    Generated File Preview
                </div>
            </div>

            <div className="flex-1 min-h-0 mt-4 overflow-auto rounded-md border border-outline/20 bg-surface-container-low">
                <pre className="p-4 text-sm text-textcolor font-mono whitespace-pre overflow-x-auto">
                    {generatedCode}
                </pre>
            </div>

            <div className="w-full flex-shrink-0 flex items-center justify-center gap-6 py-4 border-t border-outline/20 mt-4">
                <Button onClick={onCancel}>Cancel</Button>
                <Button
                    variant="primary"
                    onClick={onApprove}
                    disabled={loading}
                >
                    {loading ? 'Writing...' : 'Approve & Overwrite'}
                </Button>
            </div>
        </div>
    );
};
