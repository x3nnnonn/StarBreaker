namespace StarBreaker.CryChunkFile;

public enum ChunkTypeChCf : ushort
{
    //this any makes no sense
    Any = 0x0,
    Mesh = 0x1000,
    Helper = 0x1001,
    BoneAnim = 0x1003,
    BoneNameList = 0x1005,
    SceneProps = 0x1008,
    Node = 0x100B,
    Controller = 0x100D,
    Timing = 0x100E,
    BoneMesh = 0x100F,
    MeshMorphTarget = 0x1011,
    SourceInfo = 0x1013,
    MtlName = 0x1014,
    ExportFlags = 0x1015,
    DataStream = 0x1016,
    MeshSubsets = 0x1017,
    MeshPhysicsData = 0x1018,
    
    //these are *probably* new versions of the above?
    Unknown17 = 0x3005,
    Unknown18 = 0x300A,
    Unknown19 = 0x4000,
    Unknown20 = 0x4001,
    Unknown21 = 0x4002,
    Unknown22 = 0x4007,
    Unknown23 = 0x5000,
}