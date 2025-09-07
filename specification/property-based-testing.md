# Minimal Property-Based Testing - PBR Material Dump Tool

## Realitätscheck ✅
Das Tool **funktioniert bereits stabil**. Property-Based Testing sollte nur für **kritische Kernfunktionen** eingesetzt werden, nicht für jede Hilfsfunktion.

## Fokus auf Essentials (nicht Over-Engineering)

## Framework Setup

### C# Property-Based Testing mit FsCheck
```csharp
// MaterialDumpTool.Tests.csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.0.0" />
    <PackageReference Include="xunit" Version="2.4.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.4.3" />
    <PackageReference Include="FsCheck" Version="2.16.4" />
    <PackageReference Include="FsCheck.Xunit" Version="2.16.4" />
    <PackageReference Include="SixLabors.ImageSharp" Version="3.1.5" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MaterialDumpTool\MaterialDumpTool.csproj" />
  </ItemGroup>
</Project>
```

## Nur 2-3 kritische Properties (nicht 15+)

### PBT-001: Dateinamen-Normalisierung (Kern-Feature)
```csharp
using FsCheck;
using FsCheck.Xunit;

public class FileNormalizationProperties
{
    [Property]
    public bool NormalizedFilenames_Always_LowerCase(NonEmptyString fileName)
    {
        // Arrange
        var input = fileName.Get + ".png";
        
        // Act
        var result = PbrMaterialConversion.NormalizeFilename(input);
        
        // Assert
        return result.Equals(result.ToLower());
    }
    
    [Property]
    public bool NormalizedFilenames_Replace_Hyphens_With_Underscores(NonEmptyString baseName)
    {
        // Arrange
        var inputWithHyphens = baseName.Get.Replace("_", "-") + "-albedo.png";
        
        // Act
        var result = PbrMaterialConversion.NormalizeFilename(inputWithHyphens);
        
        // Assert
        return !result.Contains("-albedo") && result.Contains("_albedo");
    }
    
    [Property]
    public bool Normalization_Is_Idempotent(NonEmptyString fileName)
    {
        // Arrange
        var input = fileName.Get + "_albedo.png";
        
        // Act
        var firstNormalization = PbrMaterialConversion.NormalizeFilename(input);
        var secondNormalization = PbrMaterialConversion.NormalizeFilename(firstNormalization);
        
        // Assert
        return firstNormalization == secondNormalization;
    }
    
    [Property]
    public bool Valid_PNG_Extensions_Preserved(NonEmptyString baseName)
    {
        // Arrange
        var textureTypes = new[] { "_albedo", "_ao", "_height", "_normal-ogl", "_metallic", "_roughness" };
        
        return textureTypes.All(textureType =>
        {
            var input = baseName.Get + textureType + ".png";
            var result = PbrMaterialConversion.NormalizeFilename(input);
            return result.EndsWith(".png");
        });
    }
}
```

### PBT-002: Nur 1 Bildverarbeitungs-Property (das Wichtigste)
```csharp  
public class ImageProcessingProperties
{
    [Property]
    public bool Resized_Images_Have_Correct_Dimensions(PositiveInt targetSize)
    {
        // Arrange
        var validResolutions = new[] { 8, 16, 32, 64, 128, 256, 512, 1024, 2048 };
        var resolution = validResolutions[targetSize.Get % validResolutions.Length];
        
        // Act
        using var originalImage = CreateTestImage(2048, 2048);
        using var resizedImage = ResizeImage(originalImage, resolution, resolution);
        
        // Assert
        return resizedImage.Width == resolution && resizedImage.Height == resolution;
    }
}
```

## ~~Entfernte Over-Engineering Properties~~

### ~~PBT-003: Ordnerstruktur~~ 
**Warum entfernt**: Funktioniert bereits stabil, kein kritischer Testfall

### ~~PBT-004: Fehlerbehandlung~~
**Warum entfernt**: Edge Cases, nicht kernkritisch

### ~~PBT-005: Performance~~  
**Warum entfernt**: Mikro-Optimierungen ohne praktischen Nutzen

## Custom Generators

### Material Name Generator
```csharp
public static class MaterialGenerators
{
    public static Arbitrary<string> ValidMaterialNames()
    {
        return Arb.From(
            Gen.Elements("pbr_material", "stone_wall", "metal_surface", "wood_floor")
                .Select(baseName => baseName + "_" + Gen.Choose(1, 999).Sample(0, 1).First())
        );
    }
    
    public static Arbitrary<List<string>> TextureFileNames()
    {
        var suffixes = new[] { "_albedo", "_ao", "_height", "_normal-ogl", "_metallic", "_roughness" };
        
        return Arb.From(
            Gen.NonEmptyListOf(
                from baseName in Arb.Generate<NonEmptyString>()
                from suffix in Gen.Elements(suffixes)
                select baseName.Get + suffix + ".png"
            )
        );
    }
    
    public static Arbitrary<DirectoryInfo> ValidWorkspaceStructure()
    {
        return Arb.From(
            from materials in Gen.ListOf(ValidMaterialNames().Generator, 1, 10)
            select CreateWorkspaceStructure(materials)
        );
    }
}
```

## Minimal Test Configuration

### Simple FsCheck Setup
```csharp
public class MinimalPropertyTests
{
    [Fact]
    public void Run_Critical_Properties_Only()
    {
        // Nur die 2 kritischen Properties
        Check.One(new FileNormalizationProperties().NormalizedFilenames_Always_LowerCase);
        Check.One(new FileNormalizationProperties().Normalization_Is_Idempotent);
        Check.One(new ImageProcessingProperties().Resized_Images_Have_Correct_Dimensions);
    }
}
```

**Total Aufwand**: ~1 Stunde implementieren, ~30 Minuten bei jeder Änderung laufen lassen

## Invarianten und Eigenschaften

### Nur 3 kritische Invarianten
1. **Auflösungs-Invarianz**: Zielauflösung wird exakt erreicht ✅
2. **Namens-Konsistenz**: Normalisierte Namen bleiben konsistent ✅  
3. **Format-Invarianz**: PNG bleibt PNG ✅

**Das war's!** Alles andere funktioniert bereits und braucht keine Property-Tests.

## CI/CD Integration

### GitHub Actions Workflow
```yaml
name: Property-Based Tests

on: [push, pull_request]

jobs:
  pbt:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '6.0'
      
      - name: Run Property-Based Tests
        run: |
          dotnet test MaterialDumpTool.Tests.csproj --logger "trx;LogFileName=pbt-results.trx"
          
      - name: Upload Test Results
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: pbt-test-results
          path: TestResults/pbt-results.trx
```

## Metriken und Reporting

### Property Coverage Metriken
- **Eigenschaftsabdeckung**: 100% aller identifizierten Invarianten
- **Generator-Diversität**: Mindestens 1000 verschiedene Eingaben pro Property
- **Edge-Case-Entdeckung**: Dokumentation aller durch PBT gefundenen Bugs

### Performance Benchmarks
- **Testausführung**: < 10 Minuten für vollständige PBT-Suite
- **Speicherverbrauch**: < 2GB während der Tests
- **Parallelisierung**: Sichere Dateisystem-Tests ohne Race Conditions