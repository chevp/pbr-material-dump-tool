# CLAUDE.md - PBR Material Dump Tool

## Projektübersicht

Das **PBR Material Dump Tool** ist eine C#-Konsolenanwendung zur effizienten Konvertierung hochauflösender PBR (Physically Based Rendering) Materialien in optimierte, skalierte Versionen. Das Tool wurde entwickelt, um die Performance von Vulkan-Rendering-Umgebungen durch schnellere Material-Ladezeiten zu verbessern.

### Technischer Stack
- **.NET 6.0** - Zielframework
- **C#** - Programmiersprache  
- **SixLabors.ImageSharp 3.1.5** - Bildverarbeitung
- **Windows x64** - Zielplattform
- **Self-contained Single-File Deployment**

## Projektstruktur

```
pbr-material-dump-tool/
├── MaterialDumpTool/
│   ├── Program.cs                 # Entry Point, CLI-Parameter Verarbeitung
│   ├── PbrMaterialConversion.cs   # Hauptlogik für Material-Konvertierung
│   └── MaterialDumpTool.csproj    # Projekt-Konfiguration
├── doc/
│   ├── software-architecture.drawio  # Architektur-Diagramm
│   └── data-flow.drawio              # Datenfluss-Diagramm
├── specification/
│   ├── use-cases.md                  # Use Cases Dokumentation
│   ├── requirements.md               # Funktionale und nicht-funktionale Anforderungen
│   ├── playwright-tests.md           # E2E Test Spezifikation
│   └── property-based-testing.md     # Property-Based Testing mit FsCheck
└── README.md                         # Projekt-Dokumentation
```

## Kern-Funktionalität

### Material-Konvertierungspipeline
1. **Eingabe-Scanning**: Durchsucht Workspace nach PBR-Material-Ordnern
2. **Datei-Validierung**: Prüft auf PNG-Dateien und Material-Konsistenz
3. **Dateinamen-Normalisierung**: Vereinheitlicht inkonsistente Namenskonventionen
4. **Multi-Resolution-Skalierung**: Erstellt 8 verschiedene Auflösungsstufen (8x8 bis 2048x2048)
5. **Ausgabe-Organisation**: Strukturierte Ablage in `_dump/` Verzeichnis

### Unterstützte PBR-Textur-Typen
- **Albedo** (`*_albedo.png`) - Basis-Farben
- **Ambient Occlusion** (`*_ao.png`) - Schattierung  
- **Height** (`*_height.png`) - Höhen-Information
- **Normal OpenGL** (`*_normal-ogl.png`) - Oberflächennormalen
- **Metallic** (`*_metallic.png`) - Metall-Eigenschaften
- **Roughness** (`*_roughness.png`) - Oberflächenrauheit
- **Metal-Roughness Combined** (`*_metalRoughness.png`) - Kombinierte Textur

## CLI-Usage

### Basis-Kommando
```bash
MaterialDumpTool.exe --dir PATH_TO_WORKSPACE
```

### Beispiele
```bash
# Spezifisches Verzeichnis
MaterialDumpTool.exe --dir "C:\Materials\PBR_Collection"

# Aktuelles Verzeichnis (Standard)  
MaterialDumpTool.exe
```

## Entwicklung und Build

### Lokale Entwicklung
```bash
# Projekt kompilieren
dotnet build MaterialDumpTool.csproj

# Debug-Ausführung
dotnet run --project MaterialDumpTool -- --dir "test-materials"

# Release Build
dotnet build -c Release
```

### Single-File Deployment
```bash
dotnet publish MaterialDumpTool.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Testing Strategy

### 1. E2E Tests mit Playwright
- **Zweck**: End-to-End CLI-Funktionalitätstests
- **Framework**: Playwright + TypeScript
- **Coverage**: Alle Use Cases und Fehlerszenarien
- **Location**: `specification/playwright-tests.md`

### 2. Property-Based Testing
- **Zweck**: Invarianten und Eigenschaften testen  
- **Framework**: FsCheck + xUnit
- **Coverage**: Dateinamen-Normalisierung, Bildverarbeitung, Ordnerstrukturen
- **Location**: `specification/property-based-testing.md`

### 3. Unit Tests (Empfohlen)
```csharp
// Beispiel Unit Test Setup
public class PbrMaterialConversionTests
{
    [Fact]
    public void NormalizeFilename_ShouldReplaceDashesWithUnderscores()
    {
        // Arrange
        var input = "material-albedo.png";
        
        // Act  
        var result = PbrMaterialConversion.NormalizeFilename(input);
        
        // Assert
        Assert.Equal("material_albedo.png", result);
    }
}
```

## Wichtige Klassen und Methoden

### `Program.cs`
- **Entry Point** für CLI-Parameter-Verarbeitung
- **Hauptmethode**: Argument-Parsing für `--dir` Parameter

### `PbrMaterialConversion.cs`
- **`run(string dir)`** - Hauptkonvertierungsprozess
- **`normalizedFilename(string srcFilename)`** - Dateinamen-Normalisierung
- **`resizeImages(string srcFolder, string destFolder, int pixels)`** - Batch-Bildverarbeitung
- **`convertAllImagesInsideFolder(string srcDir, string dumpDir)`** - Material-Pipeline

## Performance-Charakteristika

### Benchmark-Richtlinien
- **Small Collection** (1-5 Materialien): < 30 Sekunden
- **Medium Collection** (10-25 Materialien): < 2 Minuten
- **Large Collection** (50+ Materialien): < 5 Minuten

### Speicher-Management
- Streaming-basierte Bildverarbeitung mit ImageSharp
- Automatische Garbage Collection nach jeder Material-Verarbeitung
- Bounded Memory Usage (< 2GB für große Collections)

## Bekannte Issues und Limitationen

### Current Issues (aus README.md)
- Inkonsistente Namenskonventionen für Ambient Occlusion, Albedo und Metallic-Roughness Dateien
- Große Dateien sollten in kleinere Chunks aufgeteilt werden

### Technische Limitationen
- **Nur PNG-Format** unterstützt
- **Feste Auflösungsstufen** (nicht konfigurierbar)
- **Windows-spezifische** Pfad-Behandlung
- **Single-threaded** Verarbeitung

## Deployment und Distribution

### Build-Konfiguration
```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net6.0</TargetFramework>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>
```

### Distribution
- **Single Executable** (`MaterialDumpTool.exe`)
- **Self-contained** (.NET Runtime eingebettet)
- **Windows x64** spezifisch
- **Keine Installation** erforderlich

## Erweiterungsmöglichkeiten

### Zukünftige Features
1. **Multi-Format Support** (JPG, TIFF, DDS)
2. **Konfigurierbare Auflösungen** (via Config-File)
3. **Multi-Threading** für parallele Verarbeitung
4. **Cross-Platform** Support (Linux, macOS)
5. **Batch-Processing** mit JSON-Konfiguration
6. **Web-Interface** für Material-Management

### Architektur-Verbesserungen
1. **Dependency Injection** für bessere Testbarkeit
2. **Async/Await** Pattern für I/O-Operationen  
3. **Plugin-System** für erweiterte Formate
4. **Logging-Framework** (Serilog) statt Console.WriteLine

## Debugging und Troubleshooting

### Häufige Probleme
1. **"Verzeichnis nicht gefunden"** → Pfad-Validierung prüfen
2. **"Keine PNG-Dateien"** → Dateiformat und -struktur überprüfen
3. **"Zugriff verweigert"** → Administratorrechte erforderlich
4. **OutOfMemoryException** → Große Bilder in Batches verarbeiten

### Debug-Ausgaben
- **Ordner-Scanning**: `dir={i.Name}`
- **Datei-Verarbeitung**: `src={i.Name} dest={destPath}`
- **Namens-Normalisierung**: `Filename changed old: {srcFilename} new: {name}`
- **Fortschritt**: `Finished!`

## Code-Style und Konventionen

### C# Konventionen
- **XML-Dokumentation** für alle öffentlichen Methoden
- **Deutsche Kommentare** (Legacy, aber konsistent im Projekt)
- **Explicit Exception Handling** mit Console.ReadKey()
- **File-based Operations** mit using-Statements

### Datei-Namenskonventionen
- **PascalCase** für Klassen und Methoden
- **camelCase** für lokale Variablen
- **Underscore-Separation** für Material-Dateien (`material_albedo.png`)

## Material-Quellen und Testdaten

### Empfohlene PBR-Quellen
- **[TextureCan](https://www.texturecan.com/)** - Free PBR Materials
- **[CC0-Textures](https://cc0-textures.com/)** - Creative Commons Materials

### Test-Material-Struktur
```
test-materials/
├── stone_wall_001/
│   ├── stone_wall_001_albedo.png
│   ├── stone_wall_001_ao.png
│   ├── stone_wall_001_height.png
│   ├── stone_wall_001_normal-ogl.png
│   ├── stone_wall_001_roughness.png
│   └── stone_wall_001_metallic.png
└── metal_surface_002/
    └── ...
```

## Kontakt und Wartung

### Maintainer
- **Copyright**: (c) 2024 chevp
- **Lizenz**: Nicht spezifiziert (proprietär)

### Support
- **Issues**: Über Repository Issue-Tracker
- **Dokumentation**: Siehe `README.md` und `specification/` Verzeichnis