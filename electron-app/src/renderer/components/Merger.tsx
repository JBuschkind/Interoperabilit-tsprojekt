import { useEffect, useState } from 'react';
import { Button } from './Button';
import {
    DefaultDarkColors,
    DefaultLightColors,
    MisMerge3,
} from '@mismerge/react';
import { codeToHtml } from 'shiki';
import '@mismerge/core/styles.css';
import '@mismerge/core/dark.css';

type MergerProps = {
    fileName: string;
    originalCode: string | null;
    modifiedCode: string | null;
    onAcceptMerge: (mergedCode: string) => void;
    onCancelMerge?: () => void;
};

export const Merger: React.FC<MergerProps> = ({
    fileName, // TODO: Instead of showing only the current file it would be better to see the entire queue and where the user currently is. Should maybe be handled outside of component
    originalCode,
    modifiedCode,
    onAcceptMerge,
    onCancelMerge,
}) => {
    const [ctr, setCtr] = useState(originalCode);

    const [conflictsResolved, setConflictsResolved] = useState(false);

    useEffect(() => {
        console.log(ctr);
    }, [ctr]);

    const highlight = async (text: string) =>
        await codeToHtml(text, {
            lang: 'js',
            theme: 'github-dark',
        });

    return (
        <div className="flex flex-col flex-1 w-full">
            <style>
                {`
                    .mismerge {
                        font-family: 'Fira Code', monospace;
                        font-variant-ligatures: normal;
                        min-height: 75vh;
                        margin-top: 1rem;
                    }

                    .shiki {
                        background-color: transparent !important;
                    }
                `}
            </style>

            <div className="text-surface-inverse flex justify-center mb-2 gap-2">
                <span className="font-bold">File:</span> <span>{fileName}</span>
            </div>

            <div className="border border-gray-300 rounded-md w-full h-8 flex items-center justify-around">
                <div className="flex-1 text-surface-inverse text-center ">
                    Original
                </div>
                <div className="flex-1 text-surface-inverse text-center border-x border-gray-300">
                    Result
                </div>
                <div className="flex-1 text-surface-inverse text-center">
                    Generated new file
                </div>
            </div>

            <MisMerge3
                lhs={originalCode}
                ctr={ctr}
                rhs={modifiedCode}
                onCtrChange={setCtr}
                colors={DefaultDarkColors}
                wrapLines={true}
                highlight={highlight}
                conflictsResolved={conflictsResolved}
            />

            <div className="w-full flex-1 flex items-center justify-center gap-4 ">
                {/* Original Control*/}
                <div className="flex-1 flex justify-center">
                    <Button>Keep orginal</Button>
                </div>
                {/* Result Control */}
                <div className="flex-1 flex justify-around">
                    <Button
                        // disabled={!conflictsResolved} TODO: figure out how to react to this binding properly
                        onClick={() => {
                            if (ctr) onAcceptMerge(ctr);
                        }}
                    >
                        Accept Merge
                    </Button>

                    <Button onClick={onCancelMerge}>Cancel Merge</Button>
                </div>
                {/* New File Control */}
                <div className="flex-1 flex justify-center">
                    <Button>overwrite with Generated Code</Button>
                </div>
            </div>
        </div>
    );
};
