# Pragmatische Anforderungsspezifikation - PBR Material Dump Tool

## 1. Einleitung

### 1.1 Zweck
Das PBR Material Dump Tool ist ein **stabiles, funktionierendes** Kommandozeilenwerkzeug zur Konvertierung von PBR-Materialien. Focus liegt auf **pragmatischen Verbesserungen** mit minimalem Aufwand und maximalem Nutzen.

### 1.2 Geltungsbereich
Diese Spezifikation gilt für die Version 1.0 des PBR Material Dump Tools und umfasst alle funktionalen und nicht-funktionalen Anforderungen.

### 1.3 Definitionen und Abkürzungen
- **PBR**: Physically Based Rendering
- **Dump**: Optimierte, verkleinerte Kopie der ursprünglichen Materialien
- **Material**: Sammlung von Texturdateien (Albedo, Normal, Roughness, etc.)
- **CLI**: Command Line Interface

## 2. Gesamtbeschreibung

### 2.1 Produktperspektive
Das Tool arbeitet als eigenständige Anwendung und ist Teil einer Vulkan-Rendering-Pipeline zur Performance-Optimierung beim Laden von PBR-Materialien.

### 2.2 Produktfunktionen
- Batch-Konvertierung von PBR-Material-Ordnern
- Automatische Skalierung in vordefinierte Auflösungen
- Dateinamen-Normalisierung
- Fortschrittsüberwachung
- Fehlerbehandlung und Logging

### 2.3 Benutzerklassen
- **Entwickler**: Nutzen das Tool zur Asset-Vorbereitung
- **Artists**: Nutzen das Tool zur Material-Optimierung
- **Build-Systeme**: Automatisierte Integration in CI/CD-Pipelines

## 3. Funktionale Anforderungen

### 3.1 Kommandozeilenschnittstelle
**REQ-001**: Das System MUSS über eine erweiterte Kommandozeilenschnittstelle verfügen
- Parameter: `--dir PATH_TO_WORKSPACE` (funktioniert bereits ✅)
- **NEU**: `--verbose` für detailliertes Logging 
- **NEU**: `--help` für Hilfe-Information
- Standard-Verhalten: Verwendung des aktuellen Verzeichnisses

**REQ-002**: Das System MUSS Eingabeparameter robust validieren  
- Prüfung der Verzeichnisexistenz (funktioniert bereits ✅)
- **VERBESSERT**: Graceful degradation statt Hard-Exit

### 3.2 Material-Erkennung und -Verarbeitung
**REQ-003**: Das System MUSS PBR-Material-Ordner automatisch erkennen
- Durchsucht alle Unterverzeichnisse
- Ignoriert "_dump" Verzeichnisse
- Identifiziert Ordner mit PNG-Dateien

**REQ-004**: Das System MUSS folgende PBR-Textur-Typen unterstützen:
- Albedo (`*_albedo.png`)
- Ambient Occlusion (`*_ao.png`) 
- Height (`*_height.png`)
- Normal OpenGL (`*_normal-ogl.png`)
- Metallic (`*_metallic.png`)
- Roughness (`*_roughness.png`)
- Metal-Roughness Combined (`*_metalRoughness.png`)

### 3.3 Dateinamen-Normalisierung
**REQ-005**: Das System MUSS inkonsistente Dateinamen normalisieren
- Konvertierung zu Kleinbuchstaben
- Ersetzung von "-" durch "_" in Suffixen
- Spezielle Behandlung für "roughnessmetalness" → "metalRoughness"

**REQ-006**: Das System MUSS Dateinamen-Änderungen protokollieren
- Ausgabe von Alt- und Neu-Namen in der Konsole

### 3.4 Bildverarbeitung und Skalierung
**REQ-007**: Das System MUSS Bilder effizient skalieren (funktioniert bereits ✅)
- Standard-Auflösungen: 8x8 bis 2048x2048 Pixel
- **VERBESSERT**: Async/Await für bessere Performance bei großen Collections
- **OPTIONAL**: Konfigurierbare Auflösungen via Config-File

**REQ-008**: PNG-Format bleibt Standard (funktioniert bereits ✅)

**REQ-009**: ImageSharp mit **verbesserter** Memory-Verwaltung (using-Pattern)

### 3.5 Ausgabestruktur
**REQ-010**: Das System MUSS folgende Ausgabestruktur erstellen:
```
_dump/
├── [material_name]/
│   ├── 8x8/
│   ├── 16x16/
│   ├── 32x32/
│   ├── 64x64/
│   ├── 128x128/
│   ├── 256x256/
│   ├── 512x512/
│   ├── 1024x1024/
│   └── 2048x2048/
```

### 3.6 Fortschritt und Logging
**REQ-011**: Das System MUSS **verbesserten** Fortschritt anzeigen
- **NEU**: Progress Counter (X/Y Materialien)  
- **NEU**: Timestamps für bessere Nachverfolgung
- **NEU**: Verbose-Mode für Details (optional)
- Abschluss-Statistiken (Erfolg/Fehler Counts)

**REQ-012**: Das System SOLL **intelligenter** mit User Interaction umgehen
- **VERBESSERT**: Nur bei Debugger auf Input warten
- **NEU**: Automatisches Beenden in Produktionsumgebung

## 4. Nicht-funktionale Anforderungen

### 4.1 Performance
**REQ-013**: Das System SOLL **noch effizienter** große Collections verarbeiten (funktioniert bereits ✅)
- **VERBESSERT**: Async/Await für I/O-bound Operations  
- **VERBESSERT**: Using-Pattern für automatische Memory-Cleanup
- **OPTIONAL**: Parallel-Processing für Multi-Core Systeme

### 4.2 Zuverlässigkeit
**REQ-014**: Das System MUSS robust gegen Eingabefehler sein
- Behandlung nicht existierender Dateien
- Behandlung korrupter Bilddateien
- Behandlung unzureichender Dateiberechtigungen

**REQ-015**: Das System MUSS **intelligente** Fehlerbehandlung haben
- **VERBESSERT**: Continue-on-Error statt Hard-Exit
- **NEU**: Strukturiertes Logging mit Timestamps
- **VERBESSERT**: Nur kritische Fehler (OutOfMemory) beenden App

### 4.3 Benutzerfreundlichkeit
**REQ-016**: Das System MUSS aussagekräftige Konsolenausgaben bereitstellen
- Klarstellung des aktuellen Verarbeitungsschritts
- Detaillierte Fehlermeldungen

### 4.4 Portabilität
**REQ-017**: Das System MUSS als Single-File-Executable verfügbar sein
- Self-contained Deployment
- Target: Windows x64 (.NET 6.0)

### 4.5 Sicherheit
**REQ-018**: Das System DARF KEINE sensiblen Daten protokollieren
- Keine Ausgabe von Systempfaden in Logs
- Keine Übertragung von Daten über Netzwerk

### 4.6 Wartbarkeit
**REQ-019**: Der Quellcode MUSS gut dokumentiert sein
- XML-Dokumentationskommentare für alle öffentlichen Methoden
- Aussagekräftige Variablen- und Methodennamen

## 5. Systembeschränkungen

### 5.1 Technische Beschränkungen
- **.NET 6.0** Framework erforderlich
- **Windows x64** Zielplattform
- **ImageSharp 3.1.5** für Bildverarbeitung
- **PNG-Format** ausschließlich unterstützt

### 5.2 Betriebsumgebung
- Ausreichend Festplattenspeicher für Dump-Dateien (ca. 2-3x der Originalgröße)
- Dateisystem-Schreibberechtigungen im Zielverzeichnis

## 6. Pragmatische Qualitätssicherung

### 6.1 Realistische Testanforderungen  
**REQ-020**: **Minimale** funktionale Tests für neue Features
**REQ-021**: **Simple** Performance-Tests mit Standard-Materialien
**REQ-022**: **Basic** Exception-Handling Tests (nicht jeder Edge Case)

### 6.2 Akzeptanzkriterien
- Erfolgreiche Konvertierung einer Referenz-Materialsammlung
- Korrekte Ausgabestruktur und Dateigrößen
- Vollständige Fehlerbehandlung bei Edge Cases

## 7. Anhänge

### 7.1 Unterstützte PBR-Material-Quellen
- [TextureCan](https://www.texturecan.com/)
- [CC0-Textures](https://cc0-textures.com/)

### 7.2 Bekannte Limitationen
- Unterstützung nur für PNG-Format
- Feste Auflösungsstufen (nicht konfigurierbar)
- Windows-spezifische Pfad-Behandlung