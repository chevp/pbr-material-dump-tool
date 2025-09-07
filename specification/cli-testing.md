# CLI Testing Strategy - PBR Material Dump Tool

## Übersicht
**Realistische** Teststrategie für das CLI-Tool. Focus auf **kritische Funktionalität** mit angemessenen C#-Testing-Tools. Das Tool funktioniert bereits - Tests sollen **neue Features** absichern.

## Angemessene Testing-Tools für C# CLI

### xUnit + CLI Process Testing
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
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MaterialDumpTool\MaterialDumpTool.csproj" />
  </ItemGroup>
</Project>
```

## Core Test Cases (Nur das Nötigste)

### TC-001: CLI Integration Tests
```csharp
using System.Diagnostics;
using Xunit;

public class CliIntegrationTests
{
    private readonly string _toolPath = Path.Combine(
        Directory.GetCurrentDirectory(), 
        "MaterialDumpTool.exe");

    [Fact]
    public void Help_Parameter_Shows_Usage()
    {
        // Act
        var result = RunCliTool("--help");
        
        // Assert
        Assert.Contains("Usage: MaterialDumpTool.exe", result.Output);
        Assert.Contains("--dir", result.Output);
        Assert.Contains("--verbose", result.Output);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void Verbose_Mode_Provides_Detailed_Output()
    {
        // Arrange
        var testWorkspace = CreateTestWorkspace();
        
        // Act
        var result = RunCliTool($"--dir \"{testWorkspace}\" --verbose");
        
        // Assert
        Assert.Contains("[INFO]", result.Output);
        Assert.Equal(0, result.ExitCode);
        
        // Cleanup
        Directory.Delete(testWorkspace, true);
    }

    [Fact]
    public void Basic_Conversion_Succeeds()
    {
        // Arrange
        var testWorkspace = CreateTestWorkspace();
        CreateTestMaterial(testWorkspace, "test_material");
        
        // Act
        var result = RunCliTool($"--dir \"{testWorkspace}\"");
        
        // Assert
        Assert.Contains("Finished", result.Output);
        Assert.Equal(0, result.ExitCode);
        Assert.True(Directory.Exists(Path.Combine(testWorkspace, "_dump")));
        
        // Cleanup
        Directory.Delete(testWorkspace, true);
    }

    [Fact]
    public void Invalid_Directory_Returns_Error()
    {
        // Act
        var result = RunCliTool("--dir \"non-existent-path\"");
        
        // Assert
        Assert.Contains("Directory not found", result.Output);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public void Progress_Counter_Shows_Material_Processing()
    {
        // Arrange
        var testWorkspace = CreateTestWorkspace();
        CreateTestMaterial(testWorkspace, "material1");
        CreateTestMaterial(testWorkspace, "material2");
        CreateTestMaterial(testWorkspace, "material3");
        
        // Act
        var result = RunCliTool($"--dir \"{testWorkspace}\"");
        
        // Assert
        Assert.Contains("Processing material 1/3", result.Output);
        Assert.Contains("Processing material 2/3", result.Output);
        Assert.Contains("Processing material 3/3", result.Output);
        Assert.Contains("Success: 3", result.Output);
        
        // Cleanup
        Directory.Delete(testWorkspace, true);
    }
}
```

### TC-002: Unit Tests für Kernfunktionen
```csharp
public class PbrMaterialConversionTests
{
    [Theory]
    [InlineData("material-albedo.png", "material_albedo.png")]
    [InlineData("testroughnessmetalness.png", "testmetalroughness.png")]
    [InlineData("Material-AO.png", "material_ao.png")]
    public void NormalizeFilename_ReturnsExpectedResult(string input, string expected)
    {
        // Act
        var result = PbrMaterialConversion.NormalizeFilename(input);
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeFilename_Is_Idempotent()
    {
        // Arrange
        var filename = "test_albedo.png";
        
        // Act
        var first = PbrMaterialConversion.NormalizeFilename(filename);
        var second = PbrMaterialConversion.NormalizeFilename(first);
        
        // Assert
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NormalizeFilename_HandlesInvalidInput(string input)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            PbrMaterialConversion.NormalizeFilename(input));
    }
}
```

## Helper Methods

### CLI Process Runner
```csharp
public class CliTestResult
{
    public string Output { get; set; }
    public string Error { get; set; }
    public int ExitCode { get; set; }
}

private CliTestResult RunCliTool(string arguments)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = _toolPath,
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(startInfo);
    process.WaitForExit(30000); // 30 second timeout

    return new CliTestResult
    {
        Output = process.StandardOutput.ReadToEnd(),
        Error = process.StandardError.ReadToEnd(),
        ExitCode = process.ExitCode
    };
}
```

### Test Data Setup
```csharp
private string CreateTestWorkspace()
{
    var tempPath = Path.Combine(Path.GetTempPath(), "PbrTest_" + Guid.NewGuid());
    Directory.CreateDirectory(tempPath);
    return tempPath;
}

private void CreateTestMaterial(string workspace, string materialName)
{
    var materialDir = Path.Combine(workspace, materialName);
    Directory.CreateDirectory(materialDir);
    
    var textures = new[]
    {
        $"{materialName}_albedo.png",
        $"{materialName}_ao.png", 
        $"{materialName}_height.png",
        $"{materialName}_normal-ogl.png",
        $"{materialName}_roughness.png",
        $"{materialName}_metallic.png"
    };
    
    foreach (var texture in textures)
    {
        CreateDummyPNG(Path.Combine(materialDir, texture));
    }
}

private void CreateDummyPNG(string filePath)
{
    // Minimal valid PNG (1x1 transparent pixel)
    var pngBytes = new byte[]
    {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
        0x00, 0x00, 0x00, 0x0D, // IHDR length
        0x49, 0x48, 0x44, 0x52, // IHDR
        0x00, 0x00, 0x00, 0x01, // Width: 1
        0x00, 0x00, 0x00, 0x01, // Height: 1
        0x08, 0x06, 0x00, 0x00, 0x00, // Bit depth, color type, etc.
        0x1F, 0x15, 0xC4, 0x89, // CRC
        0x00, 0x00, 0x00, 0x0A, // IDAT length
        0x49, 0x44, 0x41, 0x54, // IDAT
        0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01, // Data
        0x0D, 0x0A, 0x2D, 0xB4, // CRC
        0x00, 0x00, 0x00, 0x00, // IEND length
        0x49, 0x45, 0x4E, 0x44, // IEND
        0xAE, 0x42, 0x60, 0x82  // CRC
    };
    
    File.WriteAllBytes(filePath, pngBytes);
}
```

## Test Execution Strategy

### 1. Unit Tests (5 Minuten)
- Filename normalization (bereits funktioniert)
- Basic validation logic
- **Focus**: Nur neue Logic testen

### 2. Integration Tests für neue CLI Features (10-15 Minuten)
- --help Parameter (neue Implementierung)
- --verbose Mode (neue Implementierung)  
- Progress Counter (neue Implementierung)
- Error Handling (Verbesserung)

**Gesamt-Aufwand**: ~1-2 Stunden für Tests der **neuen Features**  
**Nicht getestet**: Bereits funktionierende Kernfunktionalität

## CI/CD Integration

### GitHub Actions Workflow
```yaml
name: CLI Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '6.0'
      
      - name: Build Tool
        run: dotnet build MaterialDumpTool.csproj -c Release
      
      - name: Run Tests
        run: dotnet test MaterialDumpTool.Tests.csproj --logger "trx;LogFileName=test-results.trx"
        
      - name: Upload Test Results
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: test-results
          path: TestResults/test-results.trx
```

## Warum keine Playwright/Selenium?

### ❌ **Playwright/Selenium** 
- **Zweck**: Web-Anwendungen mit Browser-Automation
- **Problem**: CLI-Tools laufen nicht im Browser
- **Alternative**: Process-basierte Tests mit xUnit

### ✅ **xUnit + Process Testing**
- **Zweck**: .NET Anwendungen inkl. CLI-Tools
- **Methode**: Process.Start() für CLI-Aufrufe
- **Vorteile**: Native .NET Integration, einfacher Setup

## Realistische Test Coverage

### Was getestet wird ✅
- **CLI Parameter Parsing** (neue Features)
- **Help Output** (neue Implementierung)
- **Progress Reporting** (neue Implementierung)
- **Error Handling** (Verbesserungen)
- **Basic Happy Path** (Smoke Test)

### Was NICHT getestet wird ❌
- **Bildverarbeitung Details** (ImageSharp ist bereits getestet)
- **Jeder Edge Case** (Over-Engineering)
- **Performance Benchmarks** (Tool funktioniert bereits)
- **Cross-Platform** (Windows-only Tool)

**Prinzip**: Teste neue Features, vertraue auf funktionierende Legacy-Code.

## Aufwände und ROI

| Test Type | Aufwand | Nutzen | Priorität |
|-----------|---------|---------|-----------|
| CLI Integration | 1h | Hoch | Must-Have |
| Unit Tests | 30min | Mittel | Should-Have |
| Edge Cases | 2h+ | Niedrig | Nice-to-Have |

**Total sinnvolle Tests**: 1.5 Stunden  
**Over-Engineering vermeiden**: Keine 20+ Stunden Test-Suite für 400-Zeilen-Tool