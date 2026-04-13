# How to create a CLI Tool from C#

## Prerequirements
[Install the .NET SDK](https://aka.ms/dotnet/download)

## Steps

1) Create a new CLI template and give it a name:

    ```sh
        dotnet new console -n CodeGenerator
    ```

2) Paste your C# code into the main file: ``Program.cs``

3) Go into the generated folder (`CodeGenerator`) and compile the CLI. Make sure to use the self-contained flag so that users will not have to install .NET. Also define an output folder where all the CLI files will be saved to (e.g. `publish`):
    ```sh
        # Windows
        dotnet publish -c Release -r win-x64 --self-contained true -o ./publish

        # Linux
        dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish

        # macOS
        dotnet publish -c Release -r osx-x64 --self-contained true -o ./publish
    ```

4) You can execute the ``.exe `` inside the created folder (`publish`) to run the CLI. If you want to make the CLI available in a VSCode extension, copy all files from the created ``publish`` folder to a new folder inside the `tools` folder in the vscode extension to make it available there.

