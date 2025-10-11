/**
 * Copyright (c) 2024 chevp
 */

using System;
using System.IO;

/// <summary>
/// Conversion of directory contents from PBR material files
/// to PBR dump files.
/// </summary>
public static class PbrMaterialConversion
{
    /// <summary>
    /// Starts the conversion process.
    /// </summary>
    /// <param name="dir">Working Directory</param>
    public static void run(String dir)
    {
        DirectoryOperations.CreateIfNotExist(@$"{dir}\_dump");

        DirectoryInfo place = new DirectoryInfo(dir);

        DirectoryInfo[] Directories = place.GetDirectories();

        foreach (DirectoryInfo i in Directories)
        {
            if (i.Name.Equals(@$"_dump"))
                continue;

            Console.WriteLine($"dir={i.Name}");

            try
            {
                DirectoryOperations.CreateIfNotExist(@$"{dir}\_dump\{i.Name}");

                MaterialConverter.ConvertAllImagesInsideFolder(@$"{dir}\{i.Name}", $@"_dump\{i.Name}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                Console.ReadKey();
            }
        }

        Console.WriteLine("Finished!");

        Console.ReadKey();
    }
}