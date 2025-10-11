/**
 * Copyright (c) 2024 chevp
 */

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Handles the normalization of filenames for PBR materials.
/// </summary>
public static class FileNormalizer
{
    /// <summary>
    /// Normalizes different filename conventions.
    /// </summary>
    /// <param name="srcFilename">Source filename</param>
    /// <returns>Normalized filename</returns>
    public static string NormalizeFilename(string srcFilename)
    {
        string name = srcFilename.ToLower();

        // Define the suffix replacements in a dictionary
        var replacements = new Dictionary<string, string>
        {
            { "roughnessmetalness.png", "metalRoughness.png" },
            { "-ao.png", "_ao.png" },
            { "-albedo.png", "_albedo.png" },
            { "-height.png", "_height.png" },
            { "-normal-ogl.png", "_normal-ogl.png" },
            { "-normal.png", "_normal.png" },
            { "-metallic.png", "_metallic.png" },
            { "-roughness.png", "_roughness.png" }
        };

        // Apply replacements based on matching suffixes
        foreach (var replacement in replacements)
        {
            if (name.EndsWith(replacement.Key))
            {
                name = name.Replace(replacement.Key, replacement.Value);
            }
        }

        // Log changes if the filename was modified
        if (!srcFilename.Equals(name))
        {
            Console.WriteLine($"Filename changed old: {srcFilename} new: {name}");
        }

        return name;
    }

    /// <summary>
    /// Normalizes all filenames within a directory.
    /// </summary>
    /// <param name="dir">Directory containing filenames to normalize</param>
    public static void NormalizeAllImageNames(String dir)
    {
        DirectoryInfo place = new DirectoryInfo(dir);
        FileInfo[] Files = place.GetFiles();

        foreach (FileInfo i in Files)
        {
            if (i.Name.EndsWith(".png"))
            {
                String newName = NormalizeFilename((i.Name));

                if (!newName.Equals(i.Name))
                {
                    File.Move(@$"{dir}\{i.Name}", @$"{dir}\{newName}");
                }
            }
        }
    }
}