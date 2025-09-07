# Architecture Refactoring - PBR Material Dump Tool

## Übersicht
Dieses Dokument beschreibt eine umfassende Architektur-Refaktorierung der aktuellen `PbrMaterialConversion`-Klasse zu einer modularen, testbaren und erweiterbaren Lösung basierend auf SOLID-Prinzipien und modernen C#-Patterns.

## Aktuelle Architektur-Analyse

### ✅ Positive Aspekte
- **Klare Verantwortlichkeit**: Eine Klasse für Material-Konvertierung
- **Funktionale Dekomposition**: Gut aufgeteilte private Methoden
- **Robuste Bildverarbeitung**: ImageSharp Integration
- **Fehlerbehandlung**: Try-catch mit Benutzerinteraktion

### ❌ Identifizierte Probleme

#### 1. Monolithische Static-Klasse
```csharp
// Problem: Alles in einer statischen Klasse
public static class PbrMaterialConversion
{
    public static void run(String dir) { ... }
    // 400+ Zeilen Code in einer Klasse
}
```

#### 2. Single Responsibility Principle Verletzung
- **Dateioperationen** (copyFile, moveFile, createIfNotExist)
- **Bildverarbeitung** (resize, resizeImages)  
- **Logging** (Console.WriteLine überall)
- **UI/UX** (Console.ReadKey, Benutzerinteraktion)
- **Business Logic** (Material-Pipeline)

#### 3. Tight Coupling & Testability Issues
- Statische Methoden schwer zu mocken
- Direkte Dateisystem-Zugriffe
- Keine Dependency Injection
- Hardcoded Console-Ausgaben

#### 4. Primitive Obsession
```csharp
// Strings statt Value Objects
private static string normalizedFilename(string srcFilename)
private static void processSingleFileFormat(String dir, Int32 pixels)
```

#### 5. Synchrone Verarbeitung
- Keine async/await Pattern
- Kein Parallelism für Batch-Processing
- Keine Cancellation-Unterstützung

## Ziel-Architektur

### Domain-Driven Design Approach
```
MaterialDumpTool/
├── Core/                           # Domain Layer
│   ├── Domain/                     # Value Objects & Entities
│   │   ├── MaterialName.cs
│   │   ├── Resolution.cs
│   │   ├── TextureType.cs
│   │   ├── ProcessingResult.cs
│   │   └── MaterialCollection.cs
│   ├── Interfaces/                 # Contracts
│   │   ├── IMaterialProcessor.cs
│   │   ├── IImageProcessor.cs
│   │   ├── IFileSystemService.cs
│   │   ├── ILogger.cs
│   │   └── IProgressReporter.cs
│   └── Commands/                   # Command Pattern
│       ├── ProcessingCommand.cs
│       ├── ProcessMaterialCommand.cs
│       ├── ResizeImageCommand.cs
│       └── MaterialProcessingPipeline.cs
├── Application/                    # Application Layer
│   ├── Services/
│   │   ├── MaterialProcessingService.cs
│   │   └── MaterialDiscoveryService.cs
│   ├── Options/
│   │   ├── ProcessingOptions.cs
│   │   └── ConfigurationExtensions.cs
│   └── Handlers/
│       ├── ProcessMaterialHandler.cs
│       └── ResizeImageHandler.cs
├── Infrastructure/                 # Infrastructure Layer
│   ├── ImageProcessing/
│   │   ├── ImageSharpProcessor.cs
│   │   └── ImageProcessingExtensions.cs
│   ├── FileSystem/
│   │   ├── FileSystemService.cs
│   │   └── DirectoryStructureBuilder.cs
│   └── Logging/
│       ├── ConsoleLogger.cs
│       ├── FileLogger.cs
│       └── ProgressReporter.cs
└── Program.cs                      # Composition Root
```

## 1. Value Objects & Domain Models

### MaterialName Value Object
```csharp
public record MaterialName
{
    public string Value { get; }
    
    private MaterialName(string value) => Value = value;
    
    public static MaterialName From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Material name cannot be empty", nameof(value));
            
        return new MaterialName(SanitizeValue(value));
    }
    
    private static string SanitizeValue(string value) =>
        value.ToLower()
             .Replace("-", "_")
             .Trim();
             
    public static implicit operator string(MaterialName materialName) => materialName.Value;
    public override string ToString() => Value;
}
```

### Resolution Value Object
```csharp
public record Resolution(int Width, int Height)
{
    public static readonly Resolution[] StandardResolutions = 
    {
        new(8, 8), new(16, 16), new(32, 32), new(64, 64),
        new(128, 128), new(256, 256), new(512, 512), 
        new(1024, 1024), new(2048, 2048)
    };
    
    public Resolution(int square) : this(square, square) { }
    
    public bool IsSquare => Width == Height;
    public int Area => Width * Height;
    public string DirectoryName => $"{Width}x{Height}";
    
    public static Resolution Parse(string input)
    {
        var parts = input.Split('x', 'X');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var w) || !int.TryParse(parts[1], out var h))
            throw new FormatException($"Invalid resolution format: {input}");
            
        return new Resolution(w, h);
    }
}
```

### TextureType Value Object
```csharp
public record TextureType(string Suffix, string DisplayName)
{
    public static readonly TextureType Albedo = new("_albedo", "Albedo");
    public static readonly TextureType AO = new("_ao", "Ambient Occlusion");
    public static readonly TextureType Height = new("_height", "Height");
    public static readonly TextureType Normal = new("_normal-ogl", "Normal (OpenGL)");
    public static readonly TextureType Metallic = new("_metallic", "Metallic");
    public static readonly TextureType Roughness = new("_roughness", "Roughness");
    public static readonly TextureType MetalRoughness = new("_metalRoughness", "Metal-Roughness Combined");
    
    public static readonly TextureType[] All = 
    {
        Albedo, AO, Height, Normal, Metallic, Roughness, MetalRoughness
    };
    
    public bool MatchesFilename(string filename) =>
        filename.ToLower().Contains(Suffix.ToLower());
        
    public string CreateFilename(MaterialName materialName) =>
        $"{materialName}{Suffix}.png";
}
```

### ProcessingResult Domain Model
```csharp
public record ProcessingResult
{
    public bool IsSuccess { get; init; }
    public MaterialName? ProcessedMaterial { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
    public TimeSpan ProcessingTime { get; init; }
    public int ProcessedImages { get; init; }
    public long TotalBytes { get; init; }
    
    public static ProcessingResult Success(
        MaterialName material, 
        TimeSpan processingTime, 
        int imageCount,
        long bytes) =>
        new() 
        { 
            IsSuccess = true, 
            ProcessedMaterial = material, 
            ProcessingTime = processingTime,
            ProcessedImages = imageCount,
            TotalBytes = bytes
        };
        
    public static ProcessingResult Failure(
        MaterialName? material,
        string errorMessage, 
        Exception? exception = null) =>
        new() 
        { 
            IsSuccess = false, 
            ProcessedMaterial = material,
            ErrorMessage = errorMessage, 
            Exception = exception 
        };
}
```

## 2. Service Interfaces

### Core Processing Interface
```csharp
public interface IMaterialProcessor
{
    Task<ProcessingResult> ProcessMaterialAsync(
        MaterialName materialName, 
        DirectoryInfo sourceDirectory,
        ProcessingOptions options,
        CancellationToken cancellationToken = default);
        
    Task<IEnumerable<ProcessingResult>> ProcessMaterialsAsync(
        string workspaceDirectory,
        ProcessingOptions options,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}
```

### Image Processing Interface
```csharp
public interface IImageProcessor
{
    Task<Image> ResizeImageAsync(
        Image sourceImage, 
        Resolution targetResolution, 
        CancellationToken cancellationToken = default);
        
    Task ResizeAndSaveAsync(
        FileInfo sourceFile,
        FileInfo targetFile, 
        Resolution targetResolution,
        CancellationToken cancellationToken = default);
        
    Task<bool> ValidateImageAsync(FileInfo imageFile);
    
    Task<ImageMetadata> GetImageMetadataAsync(FileInfo imageFile);
}
```

### File System Interface
```csharp
public interface IFileSystemService
{
    IEnumerable<DirectoryInfo> GetMaterialDirectories(string workspacePath);
    
    Task<bool> CreateMaterialDumpStructureAsync(
        string basePath, 
        MaterialName materialName, 
        Resolution[] resolutions);
        
    Task<IEnumerable<FileInfo>> GetTextureFilesAsync(DirectoryInfo materialDirectory);
    
    Task<bool> CopyFileAsync(
        FileInfo source, 
        FileInfo destination, 
        bool overwrite = true);
        
    Task<string> NormalizeFilenameAsync(string filename);
    
    bool IsValidWorkspace(string path);
}
```

### Logging & Progress Interface
```csharp
public interface ILogger
{
    void LogDebug(string message, params object[] args);
    void LogInfo(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, Exception? exception = null, params object[] args);
}

public interface IProgressReporter
{
    void ReportProgress(ProgressReport report);
}

public record ProgressReport(
    int CompletedMaterials,
    int TotalMaterials,
    MaterialName? CurrentMaterial,
    string? CurrentOperation,
    TimeSpan ElapsedTime);
```

## 3. Command Pattern Implementation

### Abstract Command Base
```csharp
public abstract record ProcessingCommand;

public record ProcessMaterialCommand(
    MaterialName MaterialName,
    DirectoryInfo SourceDirectory,
    DirectoryInfo OutputDirectory,
    Resolution[] TargetResolutions) : ProcessingCommand;

public record ResizeImageCommand(
    FileInfo SourceFile,
    FileInfo TargetFile,
    Resolution TargetResolution) : ProcessingCommand;

public record NormalizeFilenamesCommand(
    DirectoryInfo Directory) : ProcessingCommand;
```

### Command Processor Interface
```csharp
public interface ICommandProcessor
{
    Task<ProcessingResult> ExecuteAsync<T>(T command, CancellationToken cancellationToken = default) 
        where T : ProcessingCommand;
}
```

### Material Processing Pipeline
```csharp
public class MaterialProcessingPipeline
{
    private readonly ICommandProcessor _commandProcessor;
    private readonly ILogger _logger;
    private readonly IProgressReporter _progressReporter;
    
    public MaterialProcessingPipeline(
        ICommandProcessor commandProcessor,
        ILogger logger,
        IProgressReporter progressReporter)
    {
        _commandProcessor = commandProcessor;
        _logger = logger;
        _progressReporter = progressReporter;
    }
    
    public async Task<PipelineResult> ExecuteAsync(
        IEnumerable<ProcessingCommand> commands,
        PipelineOptions options,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProcessingResult>();
        var startTime = DateTime.UtcNow;
        
        var commandList = commands.ToList();
        var semaphore = new SemaphoreSlim(options.MaxConcurrency);
        
        var tasks = commandList.Select(async (command, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                _progressReporter.ReportProgress(new ProgressReport(
                    CompletedMaterials: index,
                    TotalMaterials: commandList.Count,
                    CurrentMaterial: ExtractMaterialName(command),
                    CurrentOperation: command.GetType().Name,
                    ElapsedTime: DateTime.UtcNow - startTime
                ));
                
                var result = await _commandProcessor.ExecuteAsync(command, cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Command execution failed", ex);
                return ProcessingResult.Failure(
                    ExtractMaterialName(command),
                    $"Command execution failed: {ex.Message}",
                    ex);
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        var completedResults = await Task.WhenAll(tasks);
        results.AddRange(completedResults);
        
        return new PipelineResult(
            results,
            DateTime.UtcNow - startTime,
            results.Count(r => r.IsSuccess),
            results.Count(r => !r.IsSuccess));
    }
    
    private static MaterialName? ExtractMaterialName(ProcessingCommand command) =>
        command switch
        {
            ProcessMaterialCommand pmc => pmc.MaterialName,
            _ => null
        };
}

public record PipelineResult(
    IReadOnlyList<ProcessingResult> Results,
    TimeSpan TotalTime,
    int SuccessCount,
    int FailureCount);

public class PipelineOptions
{
    public int MaxConcurrency { get; init; } = Environment.ProcessorCount;
    public bool FailFast { get; init; } = false;
    public bool ContinueOnError { get; init; } = true;
}
```

## 4. Configuration & Options

### Processing Options
```csharp
public class ProcessingOptions
{
    public Resolution[] TargetResolutions { get; init; } = Resolution.StandardResolutions;
    public bool OverwriteExisting { get; init; } = true;
    public bool NormalizeFilenames { get; init; } = true;
    public bool ValidateImages { get; init; } = true;
    public int MaxConcurrency { get; init; } = Environment.ProcessorCount;
    public TextureType[] RequiredTextures { get; init; } = TextureType.All;
    public string OutputDirectoryName { get; init; } = "_dump";
    public ImageQuality ImageQuality { get; init; } = ImageQuality.High;
    public bool CreateBackups { get; init; } = false;
    public LogLevel LogLevel { get; init; } = LogLevel.Info;
}

public enum ImageQuality
{
    Low = 1,
    Medium = 2, 
    High = 3,
    Lossless = 4
}

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}
```

### Configuration Extensions
```csharp
public static class ConfigurationExtensions
{
    public static IServiceCollection AddMaterialProcessing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ProcessingOptions>(
            configuration.GetSection("MaterialProcessing"));
            
        services.AddSingleton<IMaterialProcessor, MaterialProcessor>();
        services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<ILogger, ConsoleLogger>();
        services.AddSingleton<IProgressReporter, ConsoleProgressReporter>();
        services.AddSingleton<ICommandProcessor, CommandProcessor>();
        services.AddSingleton<MaterialProcessingPipeline>();
        
        return services;
    }
}
```

## 5. Implementation Examples

### Material Processor Service
```csharp
public class MaterialProcessor : IMaterialProcessor
{
    private readonly IImageProcessor _imageProcessor;
    private readonly IFileSystemService _fileSystem;
    private readonly ILogger _logger;
    private readonly ProcessingOptions _options;
    
    public MaterialProcessor(
        IImageProcessor imageProcessor,
        IFileSystemService fileSystem,
        ILogger logger,
        IOptions<ProcessingOptions> options)
    {
        _imageProcessor = imageProcessor;
        _fileSystem = fileSystem;
        _logger = logger;
        _options = options.Value;
    }
    
    public async Task<ProcessingResult> ProcessMaterialAsync(
        MaterialName materialName,
        DirectoryInfo sourceDirectory,
        ProcessingOptions options,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInfo("Processing material: {MaterialName}", materialName);
            
            // 1. Create directory structure
            var outputPath = Path.Combine(sourceDirectory.Parent!.FullName, options.OutputDirectoryName);
            await _fileSystem.CreateMaterialDumpStructureAsync(
                outputPath, 
                materialName, 
                options.TargetResolutions);
            
            // 2. Get texture files
            var textureFiles = await _fileSystem.GetTextureFilesAsync(sourceDirectory);
            
            // 3. Normalize filenames if required
            if (options.NormalizeFilenames)
            {
                foreach (var file in textureFiles)
                {
                    var normalizedName = await _fileSystem.NormalizeFilenameAsync(file.Name);
                    if (normalizedName != file.Name)
                    {
                        var newPath = Path.Combine(file.Directory!.FullName, normalizedName);
                        file.MoveTo(newPath);
                        _logger.LogInfo("Normalized filename: {OldName} -> {NewName}", file.Name, normalizedName);
                    }
                }
            }
            
            // 4. Process each resolution
            var processedImages = 0;
            var totalBytes = 0L;
            
            foreach (var resolution in options.TargetResolutions)
            {
                var resolutionDir = Path.Combine(outputPath, materialName, resolution.DirectoryName);
                Directory.CreateDirectory(resolutionDir);
                
                foreach (var textureFile in textureFiles)
                {
                    var targetFile = new FileInfo(Path.Combine(resolutionDir, textureFile.Name));
                    
                    await _imageProcessor.ResizeAndSaveAsync(
                        textureFile, 
                        targetFile, 
                        resolution, 
                        cancellationToken);
                    
                    processedImages++;
                    totalBytes += targetFile.Length;
                }
            }
            
            var processingTime = DateTime.UtcNow - startTime;
            
            _logger.LogInfo("Completed processing {MaterialName} in {Time:F2}s", 
                materialName, processingTime.TotalSeconds);
            
            return ProcessingResult.Success(materialName, processingTime, processedImages, totalBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to process material {MaterialName}", ex, materialName);
            return ProcessingResult.Failure(materialName, ex.Message, ex);
        }
    }
    
    public async Task<IEnumerable<ProcessingResult>> ProcessMaterialsAsync(
        string workspaceDirectory,
        ProcessingOptions options,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var materialDirectories = _fileSystem.GetMaterialDirectories(workspaceDirectory);
        var results = new List<ProcessingResult>();
        
        var materialList = materialDirectories.ToList();
        var totalCount = materialList.Count;
        var completedCount = 0;
        var startTime = DateTime.UtcNow;
        
        var semaphore = new SemaphoreSlim(options.MaxConcurrency);
        
        var tasks = materialList.Select(async materialDir =>
        {
            await semaphore.WaitAsync(cancellationToken);
            
            try
            {
                var materialName = MaterialName.From(materialDir.Name);
                
                progress?.Report(new ProgressReport(
                    CompletedMaterials: completedCount,
                    TotalMaterials: totalCount,
                    CurrentMaterial: materialName,
                    CurrentOperation: "Processing",
                    ElapsedTime: DateTime.UtcNow - startTime));
                
                var result = await ProcessMaterialAsync(materialName, materialDir, options, cancellationToken);
                
                Interlocked.Increment(ref completedCount);
                
                return result;
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        results.AddRange(await Task.WhenAll(tasks));
        
        return results;
    }
}
```

### ImageSharp Processor Implementation
```csharp
public class ImageSharpProcessor : IImageProcessor
{
    private readonly ILogger _logger;
    
    public ImageSharpProcessor(ILogger logger)
    {
        _logger = logger;
    }
    
    public async Task<Image> ResizeImageAsync(
        Image sourceImage, 
        Resolution targetResolution, 
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var clone = sourceImage.Clone();
            clone.Mutate(x => x.Resize(targetResolution.Width, targetResolution.Height));
            return clone;
        }, cancellationToken);
    }
    
    public async Task ResizeAndSaveAsync(
        FileInfo sourceFile,
        FileInfo targetFile,
        Resolution targetResolution,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var sourceImage = await Image.LoadAsync(sourceFile.FullName, cancellationToken);
            using var resizedImage = await ResizeImageAsync(sourceImage, targetResolution, cancellationToken);
            
            // Ensure target directory exists
            targetFile.Directory?.Create();
            
            await resizedImage.SaveAsPngAsync(targetFile.FullName, cancellationToken);
            
            _logger.LogDebug("Resized {Source} to {Target} ({Resolution})", 
                sourceFile.Name, targetFile.Name, targetResolution.DirectoryName);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to resize image {SourceFile}", ex, sourceFile.Name);
            throw;
        }
    }
    
    public async Task<bool> ValidateImageAsync(FileInfo imageFile)
    {
        try
        {
            using var image = await Image.LoadAsync(imageFile.FullName);
            return image.Width > 0 && image.Height > 0;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<ImageMetadata> GetImageMetadataAsync(FileInfo imageFile)
    {
        using var image = await Image.LoadAsync(imageFile.FullName);
        return new ImageMetadata(
            Width: image.Width,
            Height: image.Height,
            Format: image.Metadata.DecodedImageFormat?.Name ?? "Unknown",
            SizeInBytes: imageFile.Length);
    }
}

public record ImageMetadata(int Width, int Height, string Format, long SizeInBytes);
```

## 6. Dependency Injection Setup

### Program.cs (Modernized)
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        try
        {
            var processor = host.Services.GetRequiredService<IMaterialProcessor>();
            var options = host.Services.GetRequiredService<IOptions<ProcessingOptions>>();
            
            var workspaceDir = GetWorkspaceDirectory(args);
            
            if (!Directory.Exists(workspaceDir))
            {
                Console.WriteLine($"Directory not found: {workspaceDir}");
                return 1;
            }
            
            var progress = new Progress<ProgressReport>(report =>
            {
                Console.WriteLine($"Processing {report.CurrentMaterial} ({report.CompletedMaterials}/{report.TotalMaterials})");
            });
            
            var results = await processor.ProcessMaterialsAsync(
                workspaceDir, 
                options.Value, 
                progress);
            
            var successCount = results.Count(r => r.IsSuccess);
            var totalCount = results.Count();
            
            Console.WriteLine($"Finished! Processed {successCount}/{totalCount} materials successfully.");
            
            return successCount == totalCount ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
    }
    
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddMaterialProcessing(context.Configuration);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });
    
    private static string GetWorkspaceDirectory(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--dir")
                return args[i + 1];
        }
        
        return Environment.CurrentDirectory;
    }
}
```

### appsettings.json
```json
{
  "MaterialProcessing": {
    "TargetResolutions": [
      { "Width": 8, "Height": 8 },
      { "Width": 16, "Height": 16 },
      { "Width": 32, "Height": 32 },
      { "Width": 64, "Height": 64 },
      { "Width": 128, "Height": 128 },
      { "Width": 256, "Height": 256 },
      { "Width": 512, "Height": 512 },
      { "Width": 1024, "Height": 1024 },
      { "Width": 2048, "Height": 2048 }
    ],
    "MaxConcurrency": 4,
    "OverwriteExisting": true,
    "NormalizeFilenames": true,
    "ValidateImages": true,
    "OutputDirectoryName": "_dump",
    "ImageQuality": "High",
    "LogLevel": "Info"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MaterialDumpTool": "Debug"
    }
  }
}
```

## 7. Testing Strategy

### Unit Test Example
```csharp
public class MaterialProcessorTests
{
    private readonly Mock<IImageProcessor> _mockImageProcessor;
    private readonly Mock<IFileSystemService> _mockFileSystem;
    private readonly Mock<ILogger> _mockLogger;
    private readonly ProcessingOptions _options;
    private readonly MaterialProcessor _processor;
    
    public MaterialProcessorTests()
    {
        _mockImageProcessor = new Mock<IImageProcessor>();
        _mockFileSystem = new Mock<IFileSystemService>();
        _mockLogger = new Mock<ILogger>();
        _options = new ProcessingOptions();
        
        _processor = new MaterialProcessor(
            _mockImageProcessor.Object,
            _mockFileSystem.Object, 
            _mockLogger.Object,
            Options.Create(_options));
    }
    
    [Fact]
    public async Task ProcessMaterialAsync_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var materialName = MaterialName.From("test_material");
        var sourceDir = new DirectoryInfo("/test/source");
        var textureFiles = new[] { new FileInfo("/test/source/test_albedo.png") };
        
        _mockFileSystem
            .Setup(x => x.GetTextureFilesAsync(sourceDir))
            .ReturnsAsync(textureFiles);
            
        _mockFileSystem
            .Setup(x => x.CreateMaterialDumpStructureAsync(
                It.IsAny<string>(),
                materialName,
                It.IsAny<Resolution[]>()))
            .ReturnsAsync(true);
            
        _mockImageProcessor
            .Setup(x => x.ResizeAndSaveAsync(
                It.IsAny<FileInfo>(),
                It.IsAny<FileInfo>(), 
                It.IsAny<Resolution>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await _processor.ProcessMaterialAsync(
            materialName, sourceDir, _options);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(materialName, result.ProcessedMaterial);
        
        _mockImageProcessor.Verify(x => x.ResizeAndSaveAsync(
            It.IsAny<FileInfo>(),
            It.IsAny<FileInfo>(),
            It.IsAny<Resolution>(), 
            It.IsAny<CancellationToken>()), 
            Times.Exactly(Resolution.StandardResolutions.Length));
    }
}
```

## 8. Migration Strategy

### Phase 1: Grundlegende Refaktorierung (2-3 Wochen)
1. **Value Objects einführen** - MaterialName, Resolution, TextureType
2. **Interfaces definieren** - IMaterialProcessor, IImageProcessor, etc.
3. **Dependency Injection Setup** - ServiceCollection, Configuration
4. **Legacy Wrapper** - Alte API weiterhin funktional

### Phase 2: Service Implementation (3-4 Wochen)  
1. **MaterialProcessor Service** implementieren
2. **ImageSharpProcessor** mit async/await
3. **FileSystemService** mit robuster Fehlerbehandlung
4. **ConsoleLogger** mit strukturiertem Logging

### Phase 3: Advanced Features (2-3 Wochen)
1. **Command Pattern** für komplexe Workflows
2. **Pipeline Processing** mit Parallelisierung  
3. **Progress Reporting** für bessere UX
4. **Configuration System** für Flexibilität

### Phase 4: Testing & Polish (2 Wochen)
1. **Comprehensive Unit Tests** (>90% Coverage)
2. **Integration Tests** mit Test-Materialien
3. **Performance Benchmarks** 
4. **Documentation Updates**

### Backward Compatibility
```csharp
// Legacy API Wrapper für schrittweise Migration
public static class PbrMaterialConversion
{
    [Obsolete("Use IMaterialProcessor service instead")]
    public static void run(String dir)
    {
        var services = BuildLegacyServices();
        var processor = services.GetRequiredService<IMaterialProcessor>();
        
        var options = new ProcessingOptions();
        var results = processor.ProcessMaterialsAsync(dir, options).GetAwaiter().GetResult();
        
        var successCount = results.Count(r => r.IsSuccess);
        Console.WriteLine($"Processed {successCount} materials successfully.");
        
        Console.WriteLine("Finished!");
        Console.ReadKey();
    }
    
    private static IServiceProvider BuildLegacyServices()
    {
        return new ServiceCollection()
            .AddMaterialProcessing(new ConfigurationBuilder().Build())
            .BuildServiceProvider();
    }
}
```

## 9. Benefits der neuen Architektur

### ✅ Verbesserte Testbarkeit
- Alle Dependencies sind injiziert und mockbar
- Unit Tests für isolierte Komponenten möglich
- Property-Based Testing für komplexe Workflows

### ✅ Bessere Performance  
- Async/Await für I/O-bound Operations
- Parallelisierung mit konfigurierbare Concurrency
- Memory-effiziente Streaming-Verarbeitung

### ✅ Erhöhte Maintainability
- Single Responsibility Principle befolgt
- Klare Separation of Concerns
- Erweiterbare durch Plugin-Pattern

### ✅ Verbesserte Robustheit
- Result Pattern statt Exception-based Flow
- Cancellation Support für lange Operationen
- Strukturiertes Logging und Error Reporting

### ✅ Flexibilität & Konfigurierbarkeit
- Options Pattern für verschiedene Use Cases
- JSON-basierte Konfiguration
- Dependency Injection für verschiedene Implementierungen

## 10. Geschätzte Aufwände

| Phase | Beschreibung | Aufwand | Risiko |
|-------|--------------|---------|--------|
| Phase 1 | Value Objects + Interfaces | 15-20 Stunden | Niedrig |
| Phase 2 | Service Implementation | 25-35 Stunden | Mittel |
| Phase 3 | Advanced Features | 20-25 Stunden | Mittel-Hoch |
| Phase 4 | Testing & Polish | 15-20 Stunden | Niedrig |
| **Gesamt** | **Vollständige Refaktorierung** | **75-100 Stunden** | **Mittel** |

### Risiken & Mitigation
- **Complexity Creep**: Fokus auf MVP-Features, iterative Verbesserung
- **Breaking Changes**: Legacy Wrapper für Backward Compatibility  
- **Performance Regression**: Kontinuierliche Benchmarks
- **Over-Engineering**: YAGNI-Prinzip befolgen, einfach starten

Diese Refaktorierung würde das PBR Material Dump Tool von einem einfachen Script zu einer robusten, produktionstauglichen Anwendung transformieren, die für Enterprise-Umgebungen geeignet ist.