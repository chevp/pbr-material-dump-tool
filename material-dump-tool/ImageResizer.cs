/**
 * Copyright (c) 2024 chevp
 */

using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

/// <summary>
/// Handles image resizing operations for PBR materials.
/// </summary>
public static class ImageResizer
{
    /// <summary>
    /// Converts file contents to a byte array.
    /// </summary>
    public static byte[] FileToByteArray(String fileName)
    {
        byte[] buff = null;

        FileStream fs = new FileStream(fileName,
            FileMode.Open, FileAccess.Read);

        BinaryReader br = new BinaryReader(fs);

        long numBytes = new FileInfo(fileName).Length;

        buff = br.ReadBytes((int)numBytes);

        return buff;
    }

    /// <summary>
    /// Resizes an image file.
    /// </summary>
    public static void Resize(String inPath, String outPath, Int32 width, Int32 height)
    {
        using (Image image = Image.Load(FileToByteArray(inPath)))
        {
            image.Mutate(x => x.Resize(width, height));

            image.Save(outPath);
        }
    }

    /// <summary>
    /// Resizes all image files in a directory.
    /// </summary>
    public static void ResizeImages(String srcFolder, String destFolder, Int32 pixels)
    {
        DirectoryInfo place = new DirectoryInfo(srcFolder);

        FileInfo[] Files = place.GetFiles();

        foreach (FileInfo i in Files)
        {
            Console.WriteLine($"src={i.Name}");

            if (i.Name.EndsWith(".png"))
            {
                try
                {
                    Resize(@$"{srcFolder}\{i.Name}", @$"{destFolder}\{i.Name}", pixels, pixels);
                }
                catch (Exception e)
                {
                    Console.WriteLine("The process failed: {0}", e.ToString());

                    Console.ReadKey();

                    Environment.Exit(0);
                }
            }
        }
    }
}