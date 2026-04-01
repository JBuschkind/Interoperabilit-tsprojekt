# How to create a CLI Tool from c#

## Prerequirements
[Install the .NET SDK](https://aka.ms/dotnet/download)

## Steps

1) 

```sh
    dotnet new console -n HelloCli
```

2) Paste c# code into: ``Program.cs``

3) Compile the CLI (self-contained -> users don’t need .NET installed)
```sh
    # Windows
    dotnet publish -c Release -r win-x64 --self-contained true -o ./publish

    # Linux
    dotnet publish -c Release -r linux-x64 --self-contained true -o ./publish

    # macOS
    dotnet publish -c Release -r osx-x64 --self-contained true -o ./publish
```

4) Use ``.exe `` file in the ``publish`` folder 

