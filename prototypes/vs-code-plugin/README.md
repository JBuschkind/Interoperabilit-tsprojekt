# VS Code Extension


### Structure
1. The main logic of the VSCode Extension is implemented in `src/extension.ts`. 
2. The CLI tools that will parse the AML/XML to C# Code and back are stored in `tools` and are called in `extension.ts`. 
3. You can change the UI components of the VSCode extension directly inside the `package.json`

### How to run it:
You can open a new VSCode instance where the extension will be installed for development by doing the following:
- Open the `extension.ts` file
- Press ``Show and Run Commands`` in VS Code
- Press ``Debug: Start Debugging``
- Press ``VSCode Extension Development``


### How to create a new vs-code extension (already done):

Intall yo and generator-code:
```sh
npm install --global yo generator-code
```

Initialize VSCode extension: 
```sh
    yo code
```

### Links
- https://www.youtube.com/watch?v=q5V4T3o3CXE
- https://code.visualstudio.com/api/get-started/your-first-extension
- https://www.youtube.com/watch?v=4vLfXL0Rm38
