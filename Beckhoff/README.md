# XML <-> C# Parser CLI (ohne AML Engine)

Dieses Projekt ist ein reines CLI-Tool.

Es unterstuetzt zwei Uebersetzungsrichtungen:
1. Forward: XML -> C# + TXT + XML-Template
2. Reverse: C# -> aktualisierte XML

## Code ausfuehren (CLI)

Im Ordner Beckhoff ausfuehren:

### Forward (XML -> C# + TXT + XML-Template)

```powershell
dotnet run --project .\xmlParser.csproj -- --direction forward
```

### Reverse (C# -> aktualisierte XML)

```powershell
dotnet run --project .\xmlParser.csproj -- --direction reverse
```

Optional:

```powershell
dotnet run --project .\xmlParser.csproj -- --help
dotnet build .\xmlParser.csproj
```

## Voraussetzungen
1. .NET 8 SDK installiert

## Richtungs-Flag

Die Richtung wird ueber eine Flag gesetzt:
1. Empfohlen: --direction forward|reverse
2. Kurzform: -d forward|reverse
3. Legacy-Alias: --mode forward|reverse

Wenn keine Richtung angegeben ist, wird forward verwendet.

## Erweiterte Aufrufe

### Forward mit expliziten Pfaden

```powershell
dotnet run --project .\xmlParser.csproj -- --direction forward --input-xml .\Input\GVL_PLC.xml --output-cs .\Output\PlcStatusControl.generated.cs --output-txt .\Output\extracted_variables.txt --properties .\Input\plcstatus.properties --template-xml .\Output\GVL_PLC.template.xml
```

### Reverse mit expliziten Pfaden

```powershell
dotnet run --project .\xmlParser.csproj -- --direction reverse --input-cs .\Output\PlcStatusControl.generated.cs --template-xml .\Output\GVL_PLC.template.xml --output-xml .\Output\GVL_PLC.updated.xml
```

Hinweis:
Reverse uebernimmt Variablennamen aus Node-Strings wie "GVL_PLC.SomeVariable" in der C#-Datei.
Wenn diese Node-Strings nicht geaendert wurden, kann die erzeugte XML unveraendert bleiben.

Wenn `Output/GVL_PLC.template.xml` nicht existiert, nutzt das Tool automatisch `Input/GVL_PLC.xml` als Fallback-Template.

## Als EXE ausfuehren (optional)

```powershell
dotnet publish .\xmlParser.csproj -c Release -o .\publish
```

Danach z. B.:

```powershell
.\publish\xmlParser.exe --direction forward
```

## Wichtige CLI-Optionen
1. --direction oder -d: forward oder reverse
2. --mode: forward oder reverse (Legacy-Alias)
3. --input-xml: Eingabe-XML fuer Forward
4. --output-cs: C#-Ausgabe fuer Forward
5. --output-txt: TXT-Ausgabe fuer Forward
6. --properties: Properties-Datei fuer Forward
7. --template-xml: XML-Template (Forward-Ausgabe / Reverse-Eingabe)
8. --input-cs: C#-Eingabe fuer Reverse
9. --output-xml: XML-Ausgabe fuer Reverse
10. --help oder -h: Hilfe anzeigen

## Projektstruktur
1. Program.cs: Einstiegspunkt
2. src/Controller/ParsingController.cs: CLI-Argumente und Pipeline-Auswahl
3. src/Service/GvlXmlService.cs: Forward-Logik
4. src/Service/CSharpToGvlXmlService.cs: Reverse-Logik
5. src/Service/PlcStatusControlConfig.cs: Laden von plcstatus.properties

## Ausgabedateien
1. Output/PlcStatusControl.generated.cs: generierte C# Zielklasse
2. Output/extracted_variables.txt: extrahierte Variablennamen
3. Output/GVL_PLC.template.xml: XML-Template fuer Reverse
4. Output/GVL_PLC.updated.xml: Rueckuebersetzte XML-Datei

Wichtige Keys:
- `namespace`
- `enumUsing`
- `hardwareUsing`
- `className`
- `interfaceName`
- `plcControlTypeName`
- `hardwareControlPoolTypeName`
- `plcReadMethodName`
- `enumTypeName`
- `plcSystemStateSourceType`
- `plcSystemStateNode`
- `allPlcNodesPresentNode`
- `canOpenStateNode`
- `appTimestampNode`
- `appVersionNode`

Environment-Variablen mit Prefix `PLCSTATUS_` ueberschreiben Werte aus der Properties-Datei.
