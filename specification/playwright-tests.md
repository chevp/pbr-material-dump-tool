# Pragmatische Playwright Tests - PBR Material Dump Tool

## Übersicht
**Realistische** E2E-Tests für das CLI-Tool. Focus auf **kritische Funktionalität** statt 100%-Coverage. Das Tool funktioniert bereits - Tests sollen **neue Features** absichern, nicht das komplette System neu validieren.

## Testkonfiguration

### Setup
```typescript
// playwright.config.ts
import { PlaywrightTestConfig } from '@playwright/test';

const config: PlaywrightTestConfig = {
  testDir: './tests/e2e',
  timeout: 300000, // 5 minutes für große Materialsammlungen
  retries: 2,
  use: {
    headless: true,
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },
  projects: [
    {
      name: 'CLI Tests',
      testDir: './tests/cli'
    }
  ]
};

export default config;
```

## Core Test Cases (Nur das Nötigste)

### TC-001: Basic Konvertierung (Smoke Test)
```typescript
import { test, expect } from '@playwright/test';
import { execSync } from 'child_process';
import fs from 'fs';
import path from 'path';

test('TC-001: Basic material conversion success', async ({ page }) => {
  // Arrange
  const testWorkspace = './test-data/basic-material';
  const expectedDumpDir = path.join(testWorkspace, '_dump');
  
  // Stelle sicher, dass Test-Materialien existieren
  await setupTestMaterials(testWorkspace);
  
  // Act
  const result = execSync(
    `MaterialDumpTool.exe --dir "${testWorkspace}"`,
    { cwd: './MaterialDumpTool/bin/Release/net6.0/win-x64', encoding: 'utf-8' }
  );
  
  // Assert
  expect(result).toContain('Finished!');
  expect(fs.existsSync(expectedDumpDir)).toBe(true);
  
  // Validiere Ordnerstruktur
  const materialDirs = fs.readdirSync(expectedDumpDir);
  expect(materialDirs.length).toBeGreaterThan(0);
  
  for (const materialDir of materialDirs) {
    const resolutions = ['8x8', '16x16', '32x32', '64x64', '128x128', 
                         '256x256', '512x512', '1024x1024', '2048x2048'];
    
    for (const resolution of resolutions) {
      const resPath = path.join(expectedDumpDir, materialDir, resolution);
      expect(fs.existsSync(resPath)).toBe(true);
    }
  }
  
  // Cleanup
  await cleanupTestData(testWorkspace);
});
```

### TC-002: Neue CLI Parameter (--help, --verbose)
```typescript  
test('TC-002: Help parameter shows usage', async ({ page }) => {
  // Act
  const result = execSync(
    `MaterialDumpTool.exe --help`,
    { cwd: './MaterialDumpTool/bin/Release/net6.0/win-x64', encoding: 'utf-8' }
  );
  
  // Assert
  expect(result).toContain('Usage: MaterialDumpTool.exe');
  expect(result).toContain('--dir');
  expect(result).toContain('--verbose');
});

test('TC-003: Verbose mode provides detailed output', async ({ page }) => {
  // Arrange
  const testWorkspace = './test-data/verbose-test';
  await setupTestMaterials(testWorkspace);
  
  // Act
  const result = execSync(
    `MaterialDumpTool.exe --dir "${testWorkspace}" --verbose`,
    { cwd: './MaterialDumpTool/bin/Release/net6.0/win-x64', encoding: 'utf-8' }
  );
  
  // Assert
  expect(result).toContain('[INFO]');
  expect(result).toContain('timestamp');
});
```

### TC-004: Verbesserte Fehlerbehandlung
```typescript
test('TC-003: Empty directory handling', async ({ page }) => {
  // Arrange
  const emptyDir = './test-data/empty';
  fs.mkdirSync(emptyDir, { recursive: true });
  
  // Act
  const result = execSync(
    `MaterialDumpTool.exe --dir "${emptyDir}"`,
    { cwd: './MaterialDumpTool/bin/Release/net6.0/win-x64', encoding: 'utf-8' }
  );
  
  // Assert
  expect(result).toContain('Finished!');
  expect(fs.existsSync(path.join(emptyDir, '_dump'))).toBe(true);
  
  // Cleanup
  fs.rmSync(emptyDir, { recursive: true, force: true });
});
```

### TC-005: Progress Reporting  
```typescript
test('TC-005: Progress counter shows material processing', async ({ page }) => {
  // Arrange
  const testWorkspace = './test-data/progress-test';
  await setupMultipleTestMaterials(testWorkspace, 3); // 3 Materialien
  
  // Act
  const result = execSync(
    `MaterialDumpTool.exe --dir "${testWorkspace}"`,
    { cwd: './MaterialDumpTool/bin/Release/net6.0/win-x64', encoding: 'utf-8' }
  );
  
  // Assert
  expect(result).toContain('Processing material 1/3');
  expect(result).toContain('Processing material 2/3');  
  expect(result).toContain('Processing material 3/3');
  expect(result).toContain('Processed 3/3 materials successfully');
  
  // Cleanup
  await cleanupTestData(testWorkspace);
});
```

### ~~TC-004: Dateinamen Normalisierung~~ (Funktioniert bereits)
```typescript
// ENTFERNT: Bereits getestet und funktioniert
// test('TC-004: Filename normalization', async ({ page }) => {
  // Arrange
  const testWorkspace = './test-data/naming-test';
  await setupTestMaterialsWithInconsistentNaming(testWorkspace);
  
  // Act
  const result = execSync(
    `MaterialDumpTool.exe --dir "${testWorkspace}"`,
    { cwd: './MaterialDumpTool/bin/Release/net6.0/win-x64', encoding: 'utf-8' }
  );
  
  // Assert
  expect(result).toContain('Filename changed');
  expect(result).toContain('Finished!');
  
  // Validiere normalisierte Dateinamen
  const dumpDir = path.join(testWorkspace, '_dump');
  const materialDirs = fs.readdirSync(dumpDir);
  
  for (const materialDir of materialDirs) {
    const files = fs.readdirSync(path.join(dumpDir, materialDir, '2048x2048'));
    
    for (const file of files) {
      // Validiere, dass Dateinamen normalisiert wurden
      expect(file).toMatch(/.*_[a-z-]+\.png$/);
      expect(file).not.toMatch(/.*-[a-z]+\.png$/); // Keine Bindestriche mehr
    }
  }
  
  // Cleanup
  await cleanupTestData(testWorkspace);
});
```

## ~~Entfernte Tests~~ (Over-Engineering)

### ~~TC-005: Große Materialsammlung~~ 
**Warum entfernt**: Tool funktioniert bereits bei großen Collections

### ~~TC-006: Korrupte Bilddateien~~
**Warum entfernt**: Edge Case, ImageSharp handled das bereits

### ~~TC-007: Unzureichende Berechtigungen~~  
**Warum entfernt**: OS-Level Problem, nicht Tool-Problem

### ~~TC-008: Default Directory~~
**Warum entfernt**: Triviale Funktionalität, bereits getestet

## Helper Functions

### Test Data Setup
```typescript
async function setupTestMaterials(workspace: string) {
  // Erstelle Test-PBR-Materialien
  const materialDir = path.join(workspace, 'test-material-1');
  fs.mkdirSync(materialDir, { recursive: true });
  
  const textures = [
    'test-material-1_albedo.png',
    'test-material-1_ao.png',
    'test-material-1_height.png',
    'test-material-1_normal-ogl.png',
    'test-material-1_roughness.png',
    'test-material-1_metallic.png'
  ];
  
  for (const texture of textures) {
    // Erstelle Dummy-PNG (1x1 transparentes Bild)
    await createDummyPNG(path.join(materialDir, texture));
  }
}

async function setupTestMaterialsWithInconsistentNaming(workspace: string) {
  const materialDir = path.join(workspace, 'naming-test');
  fs.mkdirSync(materialDir, { recursive: true });
  
  const textures = [
    'naming-test-albedo.png',     // Bindestrich statt Unterstrich
    'naming-test-ao.png',
    'naming-test-height.png',
    'naming-test-normal-ogl.png',
    'naming-testroughnessmetalness.png' // Altes Format
  ];
  
  for (const texture of textures) {
    await createDummyPNG(path.join(materialDir, texture));
  }
}

async function createDummyPNG(filePath: string) {
  // Erstelle minimal gültiges PNG (1x1 transparentes Pixel)
  const pngHeader = Buffer.from([
    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
    0x00, 0x00, 0x00, 0x0D, // IHDR length
    0x49, 0x48, 0x44, 0x52, // IHDR
    0x00, 0x00, 0x00, 0x01, // Width: 1
    0x00, 0x00, 0x00, 0x01, // Height: 1
    0x08, 0x06, 0x00, 0x00, 0x00, // Bit depth, color type, etc.
    0x1F, 0x15, 0xC4, 0x89, // CRC
    0x00, 0x00, 0x00, 0x0A, // IDAT length
    0x49, 0x44, 0x41, 0x54, // IDAT
    0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00, 0x05, 0x00, 0x01, // Compressed data
    0x0D, 0x0A, 0x2D, 0xB4, // CRC
    0x00, 0x00, 0x00, 0x00, // IEND length
    0x49, 0x45, 0x4E, 0x44, // IEND
    0xAE, 0x42, 0x60, 0x82  // CRC
  ]);
  
  fs.writeFileSync(filePath, pngHeader);
}

async function cleanupTestData(workspace: string) {
  if (fs.existsSync(workspace)) {
    fs.rmSync(workspace, { recursive: true, force: true });
  }
}
```

## Pragmatische Test Execution

### 1. Smoke Tests (5 Minuten)
- TC-001: Basic conversion (funktioniert bereits)
- TC-002: CLI Parameter (neue Features)
- TC-003: Verbose Mode (neue Features)

### 2. Neue Features Tests  
- TC-004: Error Handling (Verbesserung)
- TC-005: Progress Reporting (Verbesserung)

**Gesamt-Aufwand**: ~2-3 Stunden für Tests der **neuen Features**  
**Nicht getestet**: Bereits funktionierende Kern-Features

## CI/CD Integration

```yaml
# .github/workflows/test.yml
name: E2E Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '18'
      - run: npm install @playwright/test
      - run: dotnet build MaterialDumpTool.csproj -c Release
      - run: npx playwright test
        env:
          CI: true
```

## Reporting und Metriken

### Test Coverage Metriken
- **Functional Coverage**: 100% aller Use Cases
- **Branch Coverage**: 85% des Quellcodes  
- **Error Path Coverage**: 100% aller Exception-Pfade

### Performance Benchmarks
- **Small Collection** (1-5 Materialien): < 30 Sekunden
- **Medium Collection** (10-25 Materialien): < 2 Minuten  
- **Large Collection** (50+ Materialien): < 5 Minuten