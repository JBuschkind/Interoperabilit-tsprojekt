import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button } from './Button';
import { DefaultDarkColors, MisMerge3 } from '@mismerge/react';
import { codeToHtml } from 'shiki';
import '@mismerge/core/styles.css';
import '@mismerge/core/dark.css';

type MergerProps = {
    fileName: string;
    originalCode: string | null;
    modifiedCode: string | null;
    onAcceptMerge: (mergedCode: string) => void;
    /** Opens the cancel confirmation (current file vs entire queue). */
    onCancelMerge?: () => void;
};

/**
 * Single MisMerge3 instance for the lifetime of this component (parent uses
 * display toggling instead of unmounting — workaround for mismerge #25).
 * @see https://github.com/BearToCode/mismerge/issues/25
 */
export const Merger: React.FC<MergerProps> = ({
    fileName, // TODO: Instead of showing only the current file it would be better to see the entire queue and where the user currently is. Should maybe be handled outside of component
    originalCode,
    modifiedCode,
    onAcceptMerge,
    onCancelMerge,
}) => {
    const [ctr, setCtr] = useState(() => originalCode ?? '');

    useEffect(() => {
        setCtr(originalCode ?? '');
    }, [fileName, originalCode, modifiedCode]);

    const mergeColors = useMemo(() => {
        return typeof structuredClone === 'function'
            ? structuredClone(DefaultDarkColors)
            : (JSON.parse(
                  JSON.stringify(DefaultDarkColors),
              ) as typeof DefaultDarkColors);
    }, []);

    const highlight = useCallback(
        (text: string) =>
            codeToHtml(text, {
                lang: 'csharp',
                theme: 'github-dark',
            }),
        [],
    );

    const lhs = originalCode ?? '';
    const rhs = modifiedCode ?? '';

    return (
        <div className="flex flex-col flex-1 w-full min-h-0">
            <style>
                {`
                    .merger-editor-host .mismerge {
                        font-family: 'Fira Code', monospace;
                        font-variant-ligatures: normal;
                        width: 100%;
                        min-height: 280px;
                        max-height: min(70vh, 720px);
                        height: min(70vh, 720px);
                        margin-top: 1rem;
                    }

                    .merger-editor-host .shiki {
                        background-color: transparent !important;
                    }
                `}
            </style>

            <div className="text-textcolor flex justify-center mb-2 gap-2 flex-shrink-0">
                <span className="font-bold">File:</span> <span>{fileName}</span>
            </div>

            <div className="border border-gray-300 rounded-md w-full h-8 flex items-center justify-around flex-shrink-0">
                <div className="flex-1 text-textcolor text-center ">
                    Original
                </div>
                <div className="flex-1 text-textcolor text-center border-x border-gray-300">
                    Result
                </div>
                <div className="flex-1 text-textcolor text-center">
                    Generated new file
                </div>
            </div>

            <div className="merger-editor-host flex-1 min-h-0 w-full flex flex-col">
                <MisMerge3
                    className="mismerge"
                    lhs={lhs}
                    ctr={ctr}
                    rhs={rhs}
                    onCtrChange={setCtr}
                    colors={mergeColors}
                    wrapLines={true}
                    highlight={highlight}
                />
            </div>

            <div className="w-full flex-shrink-0 flex flex-wrap items-center justify-center gap-4 py-4 border-t border-outline/20">
                <div className="flex-1 flex justify-center min-w-[8rem]">
                    <Button>Keep orginal</Button>
                </div>
                <div className="flex-1 flex justify-around min-w-[10rem] gap-2">
                    <Button
                        onClick={() => {
                            if (ctr) onAcceptMerge(ctr);
                        }}
                    >
                        Accept Merge
                    </Button>

                    <Button
                        onClick={() => {
                            onCancelMerge?.();
                        }}
                    >
                        Cancel Merge
                    </Button>
                </div>
                <div className="flex-1 flex justify-center min-w-[8rem]">
                    <Button>overwrite with Generated Code</Button>
                </div>
            </div>
        </div>
    );
};
