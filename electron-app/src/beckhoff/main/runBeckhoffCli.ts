import path from 'path';
import { copyFileSync, existsSync, mkdirSync } from 'fs';
import { execFile } from 'child_process';
import { promisify } from 'util';
import type { RunBeckhoffCliPayload, RunBeckhoffCliResult } from '../shared/types';

const execFileAsync = promisify(execFile);

const resolveBeckhoffProjectRoot = () => {
  if (process.env.NODE_ENV === 'development') {
    return path.resolve(__dirname, '../../../Beckhoff');
  }

  return path.join(process.resourcesPath, 'Beckhoff');
};

const ensureDirectory = (dirPath: string) => {
  mkdirSync(dirPath, { recursive: true });
};

const copyIfDifferent = (sourcePath: string, targetPath: string) => {
  const source = path.resolve(sourcePath);
  const target = path.resolve(targetPath);
  if (source === target) {
    return;
  }
  copyFileSync(source, target);
};

const validateSource = (payload: RunBeckhoffCliPayload) => {
  if (!existsSync(payload.sourceFilePath)) {
    throw new Error(`Input file not found: ${payload.sourceFilePath}`);
  }

  const extension = path.extname(payload.sourceFilePath).toLowerCase();
  if (payload.direction === 'forward' && extension !== '.xml') {
    throw new Error('Forward translation expects an XML input file.');
  }

  if (payload.direction === 'reverse' && extension !== '.cs') {
    throw new Error('Reverse translation expects a C# input file.');
  }
};

export const runBeckhoffCli = async (
  payload: RunBeckhoffCliPayload,
): Promise<RunBeckhoffCliResult> => {
  validateSource(payload);

  const projectRoot = resolveBeckhoffProjectRoot();
  const inputDir = path.join(projectRoot, 'Input');
  const outputDir = path.join(projectRoot, 'Output');
  const csprojPath = path.join(projectRoot, 'xmlParser.csproj');
  const propertiesPath = path.join(inputDir, 'plcstatus.properties');

  ensureDirectory(inputDir);
  ensureDirectory(outputDir);

  const copiedInputPath =
    payload.direction === 'forward'
      ? path.join(inputDir, 'GVL_PLC.xml')
      : path.join(inputDir, 'PlcStatusControl.generated.cs');

  copyIfDifferent(payload.sourceFilePath, copiedInputPath);

  const forwardOutputCsPath = path.join(outputDir, 'PlcStatusControl.generated.cs');
  const forwardOutputTxtPath = path.join(outputDir, 'extracted_variables.txt');
  const forwardTemplateXmlPath = path.join(outputDir, 'GVL_PLC.template.xml');
  const reverseOutputXmlPath = path.join(outputDir, 'GVL_PLC.updated.xml');

  const generatedFiles =
    payload.direction === 'forward' ? [forwardOutputCsPath] : [reverseOutputXmlPath];

  const args =
    payload.direction === 'forward'
      ? [
          'run',
          '--project',
          csprojPath,
          '--',
          '--direction',
          'forward',
          '--input-xml',
          copiedInputPath,
          '--output-cs',
          forwardOutputCsPath,
          '--output-txt',
          forwardOutputTxtPath,
          '--template-xml',
          forwardTemplateXmlPath,
          '--properties',
          propertiesPath,
        ]
      : [
          'run',
          '--project',
          csprojPath,
          '--',
          '--direction',
          'reverse',
          '--input-cs',
          copiedInputPath,
          '--output-xml',
          reverseOutputXmlPath,
        ];

  try {
    const { stdout = '', stderr = '' } = await execFileAsync('dotnet', args, {
      cwd: projectRoot,
      windowsHide: true,
      maxBuffer: 10 * 1024 * 1024,
    });

    return {
      direction: payload.direction,
      sourceFilePath: payload.sourceFilePath,
      copiedInputPath,
      generatedFiles,
      stdout,
      stderr,
    };
  } catch (error) {
    const cliError = error as {
      stdout?: string;
      stderr?: string;
      message?: string;
    };

    const stdout = cliError.stdout ?? '';
    const stderr = cliError.stderr ?? cliError.message ?? 'Unknown CLI error';

    throw new Error([
      `Beckhoff CLI failed for direction '${payload.direction}'.`,
      stdout.trim(),
      stderr.trim(),
    ]
      .filter(Boolean)
      .join('\n'));
  }
};
