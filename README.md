# PBR Material Dump Tool

A CScharp program for converting PBR (Physically Based Rendering) materials into optimized dump files for faster loading in Vulkan rendering environments.

## Quick Start

To quickly start the conversion process, run the following command in your terminal or command prompt:

```bash
START MaterialDumpTool.exe --dir PATH_TO_WORKSPACE
```

Where `PATH_TO_WORKSPACE` is the directory containing the PBR materials to be converted.

## Backstory

Rendering large PBR materials (e.g., 2024x2024 resolution) can result in slower load times in Vulkan applications. To alleviate this, the PBR Material Dump Tool converts these large assets into smaller dump files, optimizing them for quicker rendering.

## Software Architecture

The application follows a structured conversion pipeline for transforming high-resolution PBR materials into various downscaled sizes for performance optimization.

![Software-Architecture](/doc/software-architecture.svg)

## Prerequisites

Before using the tool, download the required PBR materials for conversion.

| **Material Name** | **Example Filename**          |
|-------------------|-------------------------------|
| Albedo            | `pbrmat1_albedo.png`          |
| Ambient Occlusion  | `pbrmat1_ao.png`              |
| Height            | `pbrmat1_height.png`          |
| Normal (OpenGL)    | `pbrmat1_normal-ogl.png`      |
| Preview           | `pbrmat1_preview.png`         |
| Roughness         | `pbrmat1_roughness.png`       |

## Getting Started

Ensure you have placed the list of PBR materials inside your workspace directory. The initial structure should look like this:

### Initial Folder Structure

```
├── pbrmat1
│   ├── pbrmat1_albedo.png
│   ├── pbrmat1_ao.png
│   ├── pbrmat1_height.png
│   ├── pbrmat1_normal-ogl.png
│   ├── pbrmat1_preview.png
│   └── pbrmat1_roughness.png
├── pbrmat2
│   └── ...
├── pbrmatN
```

### Start the Conversion Process

To begin converting the PBR materials into optimized dump files, run:

```bash
START MaterialDumpTool.exe --dir PATH_TO_WORKSPACE
```

Where `PATH_TO_WORKSPACE` is the directory where your PBR materials are stored.

### Folder Structure After Conversion

After running the conversion tool, you should see a `_dump` directory with downscaled versions of the original PBR materials at various resolutions:

```
├── _dump
│   ├── pbrmat1
│       ├── 8x8
│           ├── pbrmat1_albedo.png
│           ├── pbrmat1_ao.png
│           ├── pbrmat1_height.png
│           ├── pbrmat1_metallic.png
│           ├── pbrmat1_normal-ogl.png
│           └── pbrmat1_roughness.png
│       ├── 16x16
│           └── ...
│       ├── 32x32
│           └── ...
│       ├── 64x64
│           └── ...
│       ├── 128x128
│           └── ...
│       ├── 256x256
│           └── ...
│       ├── 1024x1024
│           └── ...
│       ├── 2024x2024
│           └── ...
│   └── pbrmatN
```

## Example Sources for PBR Materials

You can download PBR material examples from websites like:

- [TextureCan](https://www.texturecan.com/)

## Open Issues

- [ ] Inconsistent naming conventions for ambient occlusion, albedo, and metallic roughness files.
- [ ] Large files should be split into smaller chunks for better handling and processing.

