/**
 * Copyright (c) 2024 chevp
 */

using System;
using System.IO;

/// <summary>
/// Handles directory and file operations for PBR material conversion.
/// </summary>
public static class DirectoryOperations
{
    /// <summary>
    /// Creates a directory destDir if it does not already exist
    /// </summary>
    /// <param name="destDir">Target directory</param>
    /// <param name="delete">Delete directory if it exists</param>
    public static void CreateIfNotExist(string destDir, bool delete = false)
    {
        try
        {
            if (delete && Directory.Exists(destDir))
            {
                Directory.Delete(destDir, true);
            }

            Directory.CreateDirectory(destDir); // Safe to call even if directory already exists
        }
        catch (Exception e)
        {
            Console.WriteLine($"The process failed: {e}");
            Environment.Exit(0); // Exits without waiting for user input
        }
    }

    /// <summary>
    /// Displays the directory contents.
    /// </summary>
    /// <param name="dir">Directory to display</param>
    public static bool PrintDirectoryContent(String dir)
    {
        Console.WriteLine("List of all Files:");
        DirectoryInfo place = new DirectoryInfo(dir);
        FileInfo[] Files = place.GetFiles();
        bool hasImage = true;

        foreach (FileInfo i in Files)
        {
            if (i.Name.Contains("review"))
                continue;

            Console.WriteLine(i.Name);

            if (i.Name.EndsWith(".ignore"))
                hasImage = false;
        }

        return hasImage;
    }

    /// <summary>
    /// Returns true if a directory contains images.
    /// </summary>
    /// <param name="dir">Directory to check</param>
    public static bool HasDirectoryImages(string dir)
    {
        Console.WriteLine("List of all Files:");
        DirectoryInfo directory = new DirectoryInfo(dir);

        foreach (FileInfo file in directory.GetFiles())
        {
            Console.WriteLine(file.Name); // Added to list files as per "List of all Files"

            // Check if the file name does not contain "review" and ends with ".png"
            if (!file.Name.Contains("review") && file.Name.EndsWith(".png"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Moves a file from srcPath to destPath.
    /// </summary>
    /// <param name="srcPath">Source file path</param>
    /// <param name="destPath">Destination file path</param>
    public static void MoveFile(String srcPath, String destPath)
    {
        try
        {
            if (!File.Exists(srcPath))
            {
                // This statement ensures that the file is created,
                // but the handle is not kept.
                using (FileStream fs = File.Create(srcPath)) { }
            }

            // Ensure that the target does not exist.
            if (File.Exists(destPath))
                File.Delete(destPath);

            // Move the file.
            File.Move(srcPath, destPath);
            Console.WriteLine("{0} was moved to {1}.", srcPath, destPath);

            // See if the original exists now.
            if (File.Exists(srcPath))
            {
                Console.WriteLine("The original file still exists, which is unexpected.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("The process failed: {0}", e.ToString());

            Console.ReadKey();

            Environment.Exit(0);
        }
    }

    /// <summary>
    /// Copies a file from srcPath to destPath.
    /// </summary>
    /// <param name="srcPath">Source file path</param>
    /// <param name="destPath">Destination file path</param>
    public static void CopyFile(String srcPath, String destPath)
    {
        try
        {
            if (!File.Exists(srcPath))
            {
                // This statement ensures that the file is created,
                // but the handle is not kept.
                using (FileStream fs = File.Create(srcPath)) { }
            }

            // Ensure that the target does not exist.
            if (File.Exists(destPath))
                File.Delete(destPath);

            // Copy the file.
            File.Copy(srcPath, destPath);
            Console.WriteLine("{0} was copied to {1}.", srcPath, destPath);

            // See if the original exists now.
            if (!File.Exists(srcPath))
            {
                Console.WriteLine("The original file not exists, which is unexpected.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("The process failed: {0}", e.ToString());

            Console.ReadKey();

            Environment.Exit(0);
        }
    }

    /// <summary>
    /// Moves all image files from srcPath to destPath.
    /// </summary>
    /// <param name="srcPath">Source directory path</param>
    /// <param name="destPath">Destination directory path</param>
    public static void MoveAllImages(String srcPath, String destPath)
    {
        DirectoryInfo place = new DirectoryInfo(srcPath);

        FileInfo[] Files = place.GetFiles();

        foreach (FileInfo i in Files)
        {
            Console.WriteLine($"src={i.Name} dest={destPath}");

            if (i.Name.EndsWith(".png"))
            {
                MoveFile(@$"{srcPath}\{i.Name}", @$"{destPath}\{FileNormalizer.NormalizeFilename(i.Name)}");
            }
        }
    }

    /// <summary>
    /// Copies all images from srcPath to destPath.
    /// </summary>
    /// <param name="srcPath">Source directory</param>
    /// <param name="destPath">Destination directory</param>
    public static void CopyAllImages(String srcPath, String destPath)
    {
        DirectoryInfo place = new DirectoryInfo(srcPath);

        FileInfo[] Files = place.GetFiles();

        foreach (FileInfo i in Files)
        {
            Console.WriteLine($"src={i.Name} dest={destPath}");

            if (i.Name.EndsWith(".png"))
            {
                CopyFile(@$"{srcPath}\{i.Name}", @$"{destPath}\{i.Name}");
            }
        }
    }
}