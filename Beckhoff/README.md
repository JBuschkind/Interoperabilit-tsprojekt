# XML <-> C# Parser CLI (ohne AML Engine)

Dieses Projekt ist ein reines CLI-Tool.

Es unterstuetzt drei Uebersetzungsrichtungen:

1. **Forward**: XML -> Gvl.cs (POCO) + GvlProxy.cs (Read-Methoden) + optional XML-Template
2. **Reverse**: C# (GvlProxy.cs) + Template-XML -> aktualisierte XML (Renames werden eingespielt)
3. **Generate**: Gvl.cs (+ optional GvlProxy.cs) -> frische XML (kein Template noetig)

## Voraussetzungen je Richtung (Pflichteingaben & Outputs)

| Richtung   | Pflicht-Inputs                                  | Optional-Inputs           | Outputs                       |
| ---------- | ----------------------------------------------- | ------------------------- | ----------------------------- |
| Forward    | `--input-xml` (PLCopen-XML)                     | `--properties`            | `--output-gvl`, `--output-proxy` |
| Reverse    | `--input-cs` (C# mit `ReadValueFromPlcNode<T>("GVL.X")` Aufrufen), `--template-xml` (vorhandene PLCopen-XML als Vorlage) | —                         | `--output-xml`                |
| Generate   | `--input-gvl` (Gvl.cs)                          | `--input-proxy` (GvlProxy.cs) | `--output-xml`                |

Hinweise zu den Inputs:
- **Reverse** liest **nur** Node-Strings aus dem C# (z. B. `"GVL_PLC.Status"`) und schreibt diese Namen in das mitgelieferte **Template-XML**. Ohne Template kann Reverse nicht arbeiten — es uebernimmt die Originalstruktur und aktualisiert nur Variablennamen. Faellt das Template (`Output/GVL_PLC.template.xml`) weg, nutzt Reverse automatisch `Input/GVL_PLC.xml` als Fallback.
- **Generate** dagegen baut die XML **komplett neu** aus den C#-Klassen. Es braucht **kein** Template, kann aber den Proxy zusaetzlich nutzen, um GVL-Klassen sicher zu identifizieren.

## Code ausfuehren (CLI)

Im Ordner Beckhoff ausfuehren:

### Forward (XML -> Gvl.cs + GvlProxy.cs)

```powershell
dotnet run --project .\xmlParser.csproj -- --direction forward
```

### Reverse (C# -> aktualisierte XML, mit Template)

```powershell
dotnet run --project .\xmlParser.csproj -- --direction reverse
```

### Generate (Gvl.cs + GvlProxy.cs -> frische XML, ohne Template)

```powershell
dotnet run --project .\xmlParser.csproj -- --direction generate
```

Optional:

```powershell
dotnet run --project .\xmlParser.csproj -- --help
dotnet build .\xmlParser.csproj
```

## Voraussetzungen (System)

1. .NET 10 SDK installiert

## Richtungs-Flag

Die Richtung wird ueber eine Flag gesetzt:

1. Empfohlen: `--direction forward|reverse|generate`
2. Kurzform: `-d forward|reverse|generate`
3. Legacy-Alias: `--mode forward|reverse|generate`
4. `from-csharp` ist ein Alias fuer `generate`

Wenn keine Richtung angegeben ist, wird `forward` verwendet.

## Erweiterte Aufrufe

### Forward mit expliziten Pfaden

```powershell
dotnet run --project .\xmlParser.csproj -- `
  --direction forward `
  --input-xml .\Input\GVL_PLC.xml `
  --output-gvl .\Output\Gvl.cs `
  --output-proxy .\Output\GvlProxy.cs `
  --properties .\Input\plcstatus.properties
```

Forward extrahiert pro `<globalVars>`-Block eine POCO-Klasse und pro `<dataType>`-Block eine struct-aehnliche Klasse. Variablen mit nicht primitivem Typ landen nur im POCO; im Proxy werden dazu auskommentierte Hinweise erzeugt.

### Reverse mit expliziten Pfaden

```powershell
dotnet run --project .\xmlParser.csproj -- `
  --direction reverse `
  --input-cs .\Output\GvlProxy.cs `
  --template-xml .\Output\GVL_PLC.template.xml `
  --output-xml .\Output\GVL_PLC.updated.xml
```

Hinweise (Reverse):
- Reverse uebernimmt Variablennamen aus Node-Strings wie `"GVL_PLC.SomeVariable"` in der C#-Datei und schreibt sie in das vorhandene XML-Template.
- Wenn diese Node-Strings nicht geaendert wurden, kann die erzeugte XML unveraendert bleiben.
- Wenn `Output/GVL_PLC.template.xml` nicht existiert, nutzt das Tool automatisch `Input/GVL_PLC.xml` als Fallback-Template.
- `MixedAttrsVarList`-Bloecke (Beckhoff-spezifisch) werden bei Renames mitgepflegt, aber nicht strukturell veraendert.

### Generate mit expliziten Pfaden

```powershell
dotnet run --project .\xmlParser.csproj -- `
  --direction generate `
  --input-gvl .\Output\Gvl.cs `
  --input-proxy .\Output\GvlProxy.cs `
  --output-xml .\Output\GVL_PLC.generated.xml
```

Hinweise (Generate):
- Erzeugt eine vollstaendige PLCopen-XML aus den C#-Dateien — **kein Template noetig**.
- `--input-proxy` ist optional, hilft aber bei der Klassifizierung von GVL- vs. dataType-Klassen.
- ObjectIds werden frisch generiert (originale GUIDs sind in C# nicht erhalten).
- `fileHeader`, `contentHeader` und Tasks (PlcTask, PlcTask1ms) werden mit Defaults erzeugt.
- `OPC.UA.DA`-Attribute werden standardmaessig allen nicht-konstanten Variablen hinzugefuegt.
- Stub-Platzhalter-Klassen (markiert mit `// Stub placeholders`) werden uebersprungen.
- Generate-Output ist **kein** vollstaendiger Klon der Original-XML: POU-`<localVars>` und das `persistent`-Attribut sind in den C#-Dateien nicht enthalten und werden daher nicht erzeugt.
- Roundtrip `Gvl.cs -> XML -> Gvl.cs` ist byte-identisch.

Klassifizierungsregeln (Generate):
- Klasse wird **als Property-Typ** in einer anderen Klasse referenziert -> `<dataType>`
- Klasse erscheint im Proxy als `"X.Y"` Node-Path **oder** beginnt mit `GVL_` / `Global_` -> `<globalVars>`
- Sonst -> `<dataType>` (sicherer Default)

## Wann welche Richtung nutzen?

| Zweck                                                 | Richtung   |
| ----------------------------------------------------- | ---------- |
| Aus einer bestehenden Beckhoff-XML C#-Klassen erzeugen | Forward    |
| Nur **Variablennamen** im vorhandenen XML anpassen     | Reverse    |
| Ein komplett neues XML allein aus C#-Code bauen        | Generate   |

## Als EXE ausfuehren (optional)

```powershell
dotnet publish .\xmlParser.csproj -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish\win-x64
```

Danach z. B.:

```powershell
.\publish\win-x64\xmlParser.exe --direction generate `
  --input-gvl .\Output\Gvl.cs --input-proxy .\Output\GvlProxy.cs `
  --output-xml .\Output\GVL_PLC.generated.xml
```

Diese gebuendelte EXE ist auch das Artefakt, das die Electron-App unter
`electron-app/CLIs/beckhoff/win/xmlParser.exe` verwendet.

## Als dotnet tool installieren (optional)

Als erstes das Projekt als nupkg Tool verpacken:

```powershell
dotnet pack ./xmlParser.csproj -c Release -o ./nupkg
```

Die .nupkg-Datei kann dann zur Installation verwendet werden:

```powershell
dotnet tool install -g --add-source .\path\to\your\nupkg TwinCAT.Parser.Tool
```

Der Parser kann dann mit dem Befehl `twincatparser` verwendet werden.

## Wichtige CLI-Optionen

| Option | Richtung | Bedeutung |
| --- | --- | --- |
| `--direction`, `-d` | alle | `forward` \| `reverse` \| `generate` |
| `--mode` | alle | Legacy-Alias fuer `--direction` |
| `--input-xml` | Forward | Eingabe-XML |
| `--output-gvl` | Forward | Ausgabepfad fuer Gvl.cs |
| `--output-proxy` | Forward | Ausgabepfad fuer GvlProxy.cs |
| `--output-cs` | Forward | Legacy-Alias fuer `--output-gvl` |
| `--properties` | Forward | Properties-Datei mit Config-Defaults |
| `--input-cs` | Reverse | C#-Eingabedatei (z. B. GvlProxy.cs) |
| `--template-xml` | Reverse | XML-Template (Pflicht; Fallback auf `Input/GVL_PLC.xml`) |
| `--output-xml` | Reverse / Generate | XML-Ausgabe |
| `--input-gvl` | Generate | Gvl.cs als Eingabe |
| `--input-proxy` | Generate | (Optional) GvlProxy.cs als Eingabe |
| `--help`, `-h` | alle | Hilfe anzeigen |

Forward unterstuetzt zusaetzlich Config-Override-Flags (siehe `--help`).

## Projektstruktur

1. `Program.cs`: Einstiegspunkt (DI)
2. `src/Controller/ParsingController.cs`: CLI-Argumente und Pipeline-Auswahl
3. `src/Service/GvlXmlService.cs`: Forward-Logik
4. `src/Service/CSharpToGvlXmlService.cs`: Reverse-Logik (mit Template)
5. `src/Service/GvlCsToXmlService.cs`: Generate-Logik (XML aus C# ohne Template)
6. `src/Service/PlcOpenParser.cs`: PLCopen-XML-Parser (Forward)
7. `src/Service/GvlCodeGenerator.cs`: C#-Generator (Forward)
8. `src/Service/PlcStatusControlConfig.cs`: Laden von plcstatus.properties

## Ausgabedateien (Standardpfade)

| Datei | Erzeugt von |
| --- | --- |
| `Output/Gvl.cs` | Forward (POCO-Mirror aller globalVars/dataTypes) |
| `Output/GvlProxy.cs` | Forward (Read-Methoden pro primitiver Variable) |
| `Output/GVL_PLC.template.xml` | Forward (optional, fuer Reverse) |
| `Output/GVL_PLC.updated.xml` | Reverse |
| `Output/GVL_PLC.generated.xml` | Generate |

## Properties-Datei (Forward)

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

Prioritaet: defaults < Properties-Datei < CLI-Optionen < Env-Variablen (`PLCSTATUS_*`).

Environment-Variablen mit Prefix `PLCSTATUS_` ueberschreiben Werte aus der Properties-Datei.
