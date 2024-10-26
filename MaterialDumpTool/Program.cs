/**
 * Copyright (c) 2024 chevp
 */

using System.Diagnostics;
using System.IO;

string currDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i].Equals("--dir"))
    {
        currDir = args[i + 1];
    }
}

PbrMaterialConversion.run(currDir);