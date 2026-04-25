export type BeckhoffDirection = 'forward' | 'reverse';

export type RunBeckhoffCliPayload = {
  direction: BeckhoffDirection;
  sourceFilePath: string;
};

export type RunBeckhoffCliResult = {
  direction: BeckhoffDirection;
  sourceFilePath: string;
  copiedInputPath: string;
  generatedFiles: string[];
  stdout: string;
  stderr: string;
};
