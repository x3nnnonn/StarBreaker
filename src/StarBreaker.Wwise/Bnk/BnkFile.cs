using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StarBreaker.Common;

namespace StarBreaker.Wwise.Bnk;

public class BnkFile
{
    public uint Version { get; private set; }
    public uint Id { get; private set; }
    public BnkSection[] DataIndex { get; private set; }
    public byte[] RawData { get; private set; }
    public Dictionary<BnkSectionType, byte[]> SectionData { get; } = new();

    public static BnkFile Open(Stream stream)
    {
        var file = new BnkFile();
        var br = new BinaryReader(stream);

        while (br.BaseStream.Position < br.BaseStream.Length)
        {
            var sectionType = (BnkSectionType)br.ReadUInt32();
            var sectionSize = br.ReadUInt32();

            switch (sectionType)
            {
                case BnkSectionType.BKHD:
                    file.Version = br.ReadUInt32();
                    file.Id = br.ReadUInt32();
                    if (sectionSize > 8)
                    {
                        br.BaseStream.Seek(sectionSize - 8, SeekOrigin.Current);
                    }

                    break;

                case BnkSectionType.DIDX:
                    var count = sectionSize / Unsafe.SizeOf<BnkSection>();
                    file.DataIndex = stream.ReadArray<BnkSection>((int)count);
                    break;

                case BnkSectionType.DATA:
                    file.RawData = br.ReadBytes((int)sectionSize);
                    break;

                case BnkSectionType.HIRC:
                    var objCount = br.ReadUInt32();
                    
                    var objects = new List<BnkHircObject>((int)objCount);
                    
                    for (var i = 0; i < objCount; i++)
                    {
                        var objType = br.ReadByte();
                        var objLength = br.ReadUInt32();
                        var objId = br.ReadUInt32();
                        var objData = br.ReadBytes((int)(objLength - 4));
                        
                        if (objType == (byte)BnkHircObjectType.SoundFx)
                        {
                            var soundFx = SoundFx.Read(objData);
                            Console.WriteLine($"SoundFx: {objId}");
                        }
                        
                        objects.Add(new BnkHircObject
                        {
                            Type = (BnkHircObjectType)objType,
                            Id = objId,
                            Data = objData
                        });
                    }
                    
                    Console.WriteLine($"HIRC: {objCount} objects");
                    break;
                case BnkSectionType.INIT:
                case BnkSectionType.STMG:
                case BnkSectionType.ENVS:
                case BnkSectionType.PLAT:
                case BnkSectionType.STID:
                    byte[] sectionBytes = br.ReadBytes((int)sectionSize);
                    file.SectionData[sectionType] = sectionBytes;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(sectionType),
                        $"Unknown section type: {sectionType:X}");
            }
        }

        return file;
    }

    public void ExtractWemFiles(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        for (int i = 0; i < DataIndex.Length; i++)
        {
            var section = DataIndex[i];
            var data = new byte[section.Size];
            Array.Copy(RawData, section.Offset, data, 0, section.Size);

            string fileName = Path.Combine(outputDirectory, $"sound_{i}.wem");
            File.WriteAllBytes(fileName, data);
        }
    }

    public ReadOnlySpan<byte> GetData(BnkSection bnkSection)
    {
        return new ReadOnlySpan<byte>(RawData, (int)bnkSection.Offset, (int)bnkSection.Size);
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

public enum BnkHircObjectType : byte
{
    Settings = 1,
    SoundFx = 2,
    EventAction = 3,
    Event = 4,
    RandomContainerSequenceContainer = 5,
    SwitchContainer = 6,
    ActorMixer = 7,
    AudioBus = 8,
    BlendContainer = 9,
    MusicSegment = 10,
    MusicTrack = 11,
    MusicSwitchContainer = 12,
    MusicPlaylistContainer = 13,
    Attenuation = 14,
    DialogueEvent = 15,
    MotionBus = 16,
    MotionFX = 17,
    Effect = 18,
    AuxBus = 19,
}

[DebuggerDisplay("{Type} - {Id}")]
public class BnkHircObject
{
    public BnkHircObjectType Type { get; set; }
    public uint Id { get; set; }
    public byte[] Data { get; set; }
}

public class SoundFx
{
    public static SoundFx Read(ReadOnlySpan<byte> bytes)
    {
        var reader = new SpanReader(bytes);
        
        //var length = reader.ReadUInt32();
        //var id = reader.ReadUInt32();
        var unk = reader.ReadUInt32();
        var streamed = reader.ReadUInt32();
        var audioFileId = reader.ReadUInt32();
        var sourceId = reader.ReadUInt32();
        //if embedded in soundbank? I think this is always 0 in SC
        var soundType = reader.ReadByte();//0 = fx, 1 = voice
        var soundObject = reader.RemainingBytes;

        return new SoundFx();
    }
}