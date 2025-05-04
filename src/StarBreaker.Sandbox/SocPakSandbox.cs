﻿using System.IO.Compression;
using StarBreaker.CryChunkFile;
using StarBreaker.CryXmlB;

namespace StarBreaker.Sandbox;

public static class SocPakSandbox
{
    public static void Run()
    {
        // extract all socpaks
        var socPaks = Directory.EnumerateFiles(@"D:\StarCitizen\P4kSocPak", "*.socpak", SearchOption.AllDirectories);
        foreach (var socPak in socPaks)
        {
            var zip = new ZipArchive(File.OpenRead(socPak));
            var path = socPak.Replace(".socpak", "");
            Directory.CreateDirectory(path);
            zip.ExtractToDirectory(path);
            zip.Dispose();

            File.Delete(socPak);
        }

        //convert all entxml to xml
        var entxml = Directory.EnumerateFiles(@"D:\StarCitizen\P4kSocPak", "*.entxml", SearchOption.AllDirectories);
        foreach (var entXml in entxml)
        {
            using var fs = File.OpenRead(entXml);
            var path = entXml.Replace(".entxml", ".xml");
            if (CryXml.TryOpen(fs, out var cryXml))
                cryXml.Save(path);
        }

        //split all soc files (crychunk files)
        var socs = Directory.EnumerateFiles(@"D:\StarCitizen\P4kSocPak", "*.soc", SearchOption.AllDirectories);
        foreach (var soc in socs)
        {
            if (CrChFile.TryRead(File.ReadAllBytes(soc), out var chunkFile))
            {
                //create directory
                var path = soc.Replace(".soc", ".socParts");
                Directory.CreateDirectory(path);
                var i = 0;
                foreach (var part in chunkFile.Chunks)
                {
                    var partPath = Path.Combine(path, $"{i++}.socpart");
                    File.WriteAllBytes(partPath, part);
                }

                File.Delete(soc);
            }
        }

        //convert cryxml chunks to actual xml
        var socParts = Directory.EnumerateFiles(@"D:\StarCitizen\P4kSocPak", "*.socpart", SearchOption.AllDirectories);
        foreach (var socPart in socParts)
        {
            var fs = File.OpenRead(socPart);
            if (CryXml.TryOpen(fs, out var cryXml))
            {
                var path = socPart.Replace(".socpart", ".xml");
                cryXml.Save(path);
                fs.Dispose();
                File.Delete(socPart);
            }
        }
    }
}