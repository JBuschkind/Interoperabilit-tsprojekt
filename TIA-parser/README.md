# Structure

TODO: tbd

# Deployment

## As .exe

1. Run `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true` to deploy as an .exe
2. Take the .exe from `bin/Release/net10.0/win-x64/publish`

## As console tool

1. Run `dotnet pack -c Release -o .\nupkg`
2. You can use the TIA.parser.Tool file in the nupkg folder to install the tool
3. Run `dotnet tool install -g --add-source .\path\to\your\nupkg TIA.Parser.Tool` on your device to install the tool globally
4. Uninstall using `dotnet tool uninstall --global TIA.Parser.Tool`
