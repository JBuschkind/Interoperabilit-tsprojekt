// TODO: https://www.youtube.com/watch?v=4vLfXL0Rm38


// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import { execFile } from 'child_process';
import * as path from 'path';

// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {
	const amlLog = vscode.window.createOutputChannel("AML Import");
	
	// Use the console to output diagnostic information (console.log) and errors (console.error)
	// This line of code will only be executed once when your extension is activated
	console.log('Congratulations, your extension "aml-import" is now active!');

	// The command has been defined in the package.json file
	// Now provide the implementation of the command with registerCommand
	// The commandId parameter must match the command field in package.json
	const disposable = vscode.commands.registerCommand('aml-import.helloWorld', () => {
		// The code you place here will be executed every time your command is executed
			
		// Build path to the CLI binary
		const exePath = path.join(context.extensionPath, 'tools', 'HelloCli', 'HelloCli.exe');

		execFile(exePath, ['from', 'VSCode'], (error, stdout, stderr) => {
			if (error) {
				vscode.window.showErrorMessage(`Error: ${error.message}`);
				return;
			}
			vscode.window.showInformationMessage(stdout);
		});


	});

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
					execFile(exePath, [amlPath, outputPath], (error, stdout, stderr) => {
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
