/**
 * Copyright (c) 2024 chevp
 */

using System;
using System.IO;

/// <summary>
/// Orchestrates the conversion process of PBR materials.
/// </summary>
public static class MaterialConverter
{
    /// <summary>
    /// Executes the conversion sequence from high-resolution PBR material
    /// to multiple PBR dump files.
    /// </summary>
    /// <param name="srcDir">Source directory path</param>
    /// <param name="dumpDir">Output path for dump files</param>
    public static void ConvertAllImagesInsideFolder(string srcDir, string dumpDir)
    {
        // Check if the source directory contains images
        if (!DirectoryOperations.PrintDirectoryContent(srcDir))
        {
            Console.WriteLine($"{srcDir} has no images");
            return;
        }

        // Ensure destination folder exists
        string targetDir = Path.Combine(dumpDir, "2048x2048");
        DirectoryOperations.CreateIfNotExist(targetDir);

        // Copy images from source to dump directory if not present in source subdirectory
        if (!DirectoryOperations.HasDirectoryImages(srcDir))
        {
            DirectoryOperations.CopyAllImages(Path.Combine(srcDir, "2048x2048"), srcDir);
        }

        // Copy all images from source directory to target directory
        DirectoryOperations.CopyAllImages(srcDir, targetDir);

        // Normalize all image file names in the target directory
        FileNormalizer.NormalizeAllImageNames(targetDir);

        // Process images at progressively lower resolutions
        for (int resolution = 8; resolution <= 1024; resolution *= 2)
        {
            ProcessSingleFileFormat(dumpDir, resolution);
        }
    }

    /// <summary>
    /// Processes a single conversion step per resolution.
    /// </summary>
    /// <param name="dir">Image directory path</param>
    /// <param name="pixels">Resolution in pixels</param>
    private static void ProcessSingleFileFormat(String dir, Int32 pixels)
    {
        DirectoryOperations.CreateIfNotExist(@$"{dir}\{pixels}x{pixels}", true);
        ImageResizer.ResizeImages(@$"{dir}\2048x2048", @$"{dir}\{pixels}x{pixels}", pixels);
    }
}