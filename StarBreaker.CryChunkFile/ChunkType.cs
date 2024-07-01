namespace StarBreaker.CryChunkFile;

//from cgf-converter
public enum ChunkType : uint    // complete
{
    Any = 0x0,
    Mesh = 3435921408,
    Helper = 0xCCCC0001,
    VertAnim = 0xCCCC0002,
    BoneAnim = 0xCCCC0003,
    GeomNameList = 0xCCCC0004,
    BoneNameList = 0xCCCC0005,
    MtlList = 0xCCCC0006,
    MRM = 0xCCCC0007, //obsolete
    SceneProps = 0xCCCC0008,
    Light = 0xCCCC0009,
    PatchMesh = 0xCCCC000A,
    Node = 0xCCCC000B,
    Mtl = 0xCCCC000C,
    Controller = 0xCCCC000D,
    Timing = 0xCCCC000E,
    BoneMesh = 0xCCCC000F,
    BoneLightBinding = 0xCCCC0010,
    MeshMorphTarget = 0xCCCC0011,
    BoneInitialPos = 0xCCCC0012,
    SourceInfo = 0xCCCC0013,        // Describes the source from which the cgf was exported: source max file, machine and user.
    MtlName = 0xCCCC0014,           // provides material name as used in the material.xml file
    ExportFlags = 0xCCCC0015,       // Describes export information.
    DataStream = 0xCCCC0016,        // A data Stream
    MeshSubsets = 0xCCCC0017,       // Describes an array of mesh subsets
    MeshPhysicsData = 0xCCCC0018,   // Physicalized mesh data
    CompiledBones = 0xACDC0000,
    CompiledPhysicalBones = 0xACDC0001,
    CompiledMorphTargets = 0xACDC0002,
    CompiledPhysicalProxies = 0xACDC0003,
    CompiledIntFaces = 0xACDC0004,
    CompiledIntSkinVertices = 0xACDC0005,
    CompiledExt2IntMap = 0xACDC0006,
    BreakablePhysics = 0xACDC0007,
    FaceMap = 0xAAFC0000,           // unknown chunk
    SpeedInfo = 0xAAFC0002,         // Speed and distnace info
    FootPlantInfo = 0xAAFC0003,     // Footplant info
    BonesBoxes = 0xAAFC0004,        // unknown chunk
    FoliageInfo = 0xAAFC0005,       // unknown chunk
    GlobalAnimationHeaderCAF = 0xAAFC0007,
    
    // Star Citizen versions
    NodeSC = 0xCCCC100B,
    CompiledBonesSC = 0xCCCC1000,
    CompiledPhysicalBonesSC = 0xCCCC1001,
    CompiledMorphTargetsSC = 0xCCCC1002,
    CompiledPhysicalProxiesSC = 0xCCCC1003,
    CompiledIntFacesSC = 0xCCCC1004,
    CompiledIntSkinVerticesSC = 0xCCCC1005,
    CompiledExt2IntMapSC = 0xCCCC1006,
    UnknownSC1 = 0xCCCC2004,
    BoneBoxesSC = 0x08013004,
    // Star Citizen #ivo file chunks
    MtlNameIvo = 0x8335674E,
    MtlNameIvo320 = 0x83353333,
    CompiledBonesIvo = 0xC201973C,
    CompiledBonesIvo320 = 0xC2011111,
    CompiledPhysicalBonesUnknown = 0x90C62222,
    CompiledPhysicalBonesIvo = 0x90C687DC,  // Physics
    CompiledPhysicalBonesIvo320 = 0x90C66666,
    MeshIvo = 0x9293B9D8,           // SkinInfo
    MeshIvo320 = 0x92914444,
    IvoSkin = 0xB875B2D9,           // SkinMesh
    IvoSkin2 = 0xB8757777,
    BShapesGPU = 0x57A3BEFD,
    BShapes = 0x875CCB28,

    BinaryXmlDataSC = 0xcccbf004,
    
    

}