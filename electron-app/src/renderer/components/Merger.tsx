import { useEffect, useState } from 'react';

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
        <div className="w-full">
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

            <div className="flex justify-center mb-4 ">File: {fileName}</div>

            <div className="border border-gray-300 rounded-md w-full h-8 flex items-center justify-around">
                <div>Original</div>
                <div>Current</div>
                <div>New</div>
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

            <div className="flex items-center justify-center mt-6 gap-4">
                <button className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded">
                    Keep Original
                </button>
                <button
                    // disabled={!conflictsResolved}
                    className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded"
                    onClick={() => {
                        if (ctr) onAcceptMerge(ctr);
                    }}
                >
                    Accept Merge
                </button>

                <button
                    className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded"
                    onClick={onCancelMerge}
                >
                    Cancel Merge
                </button>
                <button className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded">
                    Overwrite with Generated Code
                </button>
            </div>
        </div>
    );
};
