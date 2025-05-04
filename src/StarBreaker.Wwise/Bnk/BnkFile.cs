using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StarBreaker.Common;

namespace StarBreaker.Wwise.Bnk;

public class BnkFile
{
    public static BnkFile Open(Stream stream)
    {
        var br = new BinaryReader(stream);

        while (br.BaseStream.Position < br.BaseStream.Length)
        {
            var sectionType = (BnkSectionType)br.ReadUInt32();
            var sectionSize = br.ReadUInt32();
            switch (sectionType)
            {
                case BnkSectionType.BKHD:
                    var version = br.ReadUInt32();
                    var id = br.ReadUInt32();
                    stream.Seek(sectionSize - 8, SeekOrigin.Current);
                    break;
                case BnkSectionType.DIDX:
                    var count = sectionSize / Unsafe.SizeOf<BnkSection>();
                    var sections = stream.ReadArray<BnkSection>((int)count);

                    break;
                case BnkSectionType.DATA:
                    break;
                case BnkSectionType.HIRC:
                    break;
                case BnkSectionType.INIT:
                    break;
                case BnkSectionType.STMG:
                    break;
                case BnkSectionType.ENVS:
                    break;
                case BnkSectionType.PLAT:
                    break;
                case BnkSectionType.STID:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            br.BaseStream.Seek(sectionSize, SeekOrigin.Current);
        }

        return new BnkFile();
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BnkSection
{
    public uint Id;
    public uint Offset;
    public uint Size;
}

public enum BnkSectionType : uint
{
    // Bank Header Section: Contains general information about the SoundBank,
    // such as its version, ID, and structure.  This is typically the first section in a .bnk file.
    BKHD = 0x44484B42, // "BKHD" in ASCII (little-endian)

    // Data Index Section: Provides an index to the data section, containing
    // offsets and sizes of the data chunks within the bank.  This allows for efficient
    // access to specific data.
    DIDX = 0x58444944, // "DIDX" in ASCII (little-endian)

    // Data Section: Contains the raw data of the SoundBank, including the audio
    // files (often in .wem format) and other binary data.
    DATA = 0x41544144, // "DATA" in ASCII (little-endian)

    // Hierarchy Section: Defines the hierarchical structure of the objects
    // within the SoundBank, such as events, sounds, and containers.
    HIRC = 0x43524948, // "HIRC" in ASCII (little-endian)

    // Initialization Section:  Contains data used to initialize the SoundBank
    // when it is loaded.  This might include global settings or state information.
    INIT = 0x54494E49, // "INIT" in ASCII (little-endian)

    // Streaming Manager Data:  Contains information related to how the audio
    // data is streamed from disk, if applicable.  This can include hints or
    // parameters for the streaming system.
    STMG = 0x474D5453, // "STMG" in ASCII (little-endian)

    // Environment Settings: Contains data related to environmental effects
    // such as reverb and occlusion.
    ENVS = 0x53564E45, // "ENVS" in ASCII (little-endian)

    // Platform Data: Contains data specific to a particular platform.
    PLAT = 0x54414C50, // "PLAT" in ASCII (little-endian)

    // Sound ID Section: Contains a list of sound IDs.
    STID = 0x44495453 // "STID" in ASCII (little-endian)
}