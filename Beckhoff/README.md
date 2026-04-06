# XML zu C# Parser (ohne AML Engine)

Dieses Projekt erzeugt aus einer PLCopen-XML Datei C#-Code und eine Variablenliste als TXT.
Es wird bewusst keine AML-Engine verwendet.

## Projektstruktur
- `Program.cs`: Einstiegspunkt
- `src/Controller/ParsingController.cs`: Schnittstelle zum Generieren der Dateien
- `src/Service/GvlXmlService.cs`: Modulare Service-Logik für XML Parsing und Code-Generierung
- `src/Service/PlcStatusControlConfig.cs`: Laden und Anwenden der `plcstatus.properties`
- `Input/GVL_PLC.xml`: Beispiel-Eingabe
- `Input/plcstatus.properties`: Konfiguration für `PlcStatusControl.generated.cs`
- `Output/`: Zielordner für generierte Dateien

## Generierte Ausgabedateien
- `Output/PlcStatusControl.generated.cs`: Zielklasse im Stil von `Ziel.cs`
- `Output/extracted_variables.txt`: Extrahierte Variablen untereinander (eine pro Zeile)

## Pipeline
Die Pipeline laeuft bei jedem Start in vier klaren Schritten:

1. Input-Phase:
	Der Controller liest die CLI-Argumente und bestimmt daraus XML-Input, Ausgabepfade und die Properties-Datei.
2. Konfigurations-Phase:
	Der Service laedt `plcstatus.properties` und kombiniert diese mit Defaults und optionalen Environment-Overrides.
3. Generierungs-Phase:
	Aus `GVL_PLC.xml` werden die relevanten Variablen und Node-Pfade extrahiert und die Datei `PlcStatusControl.generated.cs` aufgebaut.
4. Export-Phase:
	Alle extrahierten Variablennamen werden zusaetzlich als zeilenweise Liste in `extracted_variables.txt` geschrieben.

Ergebnis der Pipeline sind genau zwei Dateien im Output-Ordner: `PlcStatusControl.generated.cs` und `extracted_variables.txt`.

## Lokal ausführen
Standard (verwendet `Input/` und schreibt nach `Output/`):

```powershell
dotnet run --project .\xmlParser.csproj
```

Mit eigenen Pfaden:

```powershell
dotnet run --project .\xmlParser.csproj -- .\Input\GVL_PLC.xml .\Output\PlcStatusControl.generated.cs .\Output\extracted_variables.txt .\Input\plcstatus.properties
```

Argumentreihenfolge:
1. Eingabe XML
2. Ausgabe PlcStatusControl Klasse
3. Ausgabe TXT Variablenliste
4. Properties-Datei

## Docker ausführen

```powershell
docker compose up --build
```

Die erzeugten Dateien liegen danach in `Output/`.

## plcstatus.properties
Die Datei steuert Namespace, Typnamen und Node-Pfade für die generierte `PlcStatusControl.generated.cs`.

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

Environment-Variablen mit Prefix `PLCSTATUS_` überschreiben die Werte aus der Properties-Datei.
