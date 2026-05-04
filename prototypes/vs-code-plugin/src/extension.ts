// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import { execFile } from 'child_process';
import * as path from 'path';

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {
	const amlLog = vscode.window.createOutputChannel("AML Import");
	
	// The commandId parameter must match the command field in package.json
	const disposablePlcGen = vscode.commands.registerCommand('aml-import.generateCodeFromAML', () => {

			const exePath = path.join(context.extensionPath, 'tools', 'CodeGenerator', "CodeGenerator.exe");

			
			amlLog.appendLine("test");
			amlLog.show(true);			

			// Ask user for input AML file
			vscode.window.showOpenDialog({
				canSelectMany: false,
				filters: { 'AML files': ['aml'], 'All files': ['*'] }
			}).then(fileUris => {
				if (!fileUris || fileUris.length === 0) return;
				const amlPath = fileUris[0].fsPath;

				// Ask user for output file
				vscode.window.showSaveDialog({
					defaultUri: vscode.Uri.file(amlPath.replace('.aml', '.cs')),
					filters: { 'C# files': ['cs'], 'All files': ['*'] }
				}).then(outputUri => {
					if (!outputUri) return;
					const outputPath = outputUri.fsPath;

					// Run CLI
					execFile(exePath, [amlPath, outputPath], (error: any, stdout: any, stderr: any) => {
						if (error) {
							vscode.window.showErrorMessage(`Error: ${error.message}`);
							return;
						}
						vscode.window.showInformationMessage(stdout);
					});
				});
			});
		});

	context.subscriptions.push(disposablePlcGen, amlLog);
}

// This method is called when your extension is deactivated
export function deactivate() {}
