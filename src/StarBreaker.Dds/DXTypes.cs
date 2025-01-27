using System.Runtime.InteropServices;
using DirectXTexNet;

namespace StarBreaker.Dds;

[Flags]
public enum DdsPixelFormatFlags : uint
{
    AlphaPixels = 0x1,
    Alpha = 0x2,
    Fourcc = 0x4,
    Rgb = 0x40, // 0x00000040
    Yuv = 0x200, // 0x00000200
    Luminance = 0x20000, // 0x00020000
}

public enum CompressionAlgorithm : uint
{
    None = 0,
    DX10 = 808540228, // 0x30315844
    ATI1 = 826889281, // 0x31495441
    D3DFMT_DXT1 = 827611204, // 0x31545844
    ATI2 = 843666497, // 0x32495441
    D3DFMT_DXT2 = 844388420, // 0x32545844
    D3DFMT_DXT3 = 861165636, // 0x33545844
    D3DFMT_DXT4 = 877942852, // 0x34545844
    D3DFMT_DXT5 = 894720068, // 0x35545844
    BC4S = 1395934018, // 0x53344342
    BC5S = 1395999554, // 0x53354342
    BC4U = 1429488450, // 0x55344342
    BC5U = 1429553986, // 0x55354342
}

public enum ResourceDimension : uint
{
    D3D10_RESOURCE_DIMENSION_UNKNOWN,
    D3D10_RESOURCE_DIMENSION_BUFFER,
    D3D10_RESOURCE_DIMENSION_TEXTURE1D,
    D3D10_RESOURCE_DIMENSION_TEXTURE2D,
    D3D10_RESOURCE_DIMENSION_TEXTURE3D,
}

public struct DdsPixelFormat
{
    public uint Size;
    public DdsPixelFormatFlags PixelFormatFlags;
    public CompressionAlgorithm FourCC;
    public uint RGBBitCount;
    public uint RBitMask;
    public uint GBitMask;
    public uint BBitMask;
    public uint ABitMask;
}

[Flags]
public enum DDS_HEADER_FLAGS : uint
{
    DDS_HEADER_FLAGS_TEXTURE = 0x00001007, // DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT
    DDS_HEADER_FLAGS_MIPMAP = 0x00020000, // DDSD_MIPMAPCOUNT
    DDS_HEADER_FLAGS_VOLUME = 0x00800000, // DDSD_DEPTH
    DDS_HEADER_FLAGS_PITCH = 0x00000008, // DDSD_PITCH
    DDS_HEADER_FLAGS_LINEARSIZE = 0x00080000, // DDSD_LINEARSIZE
}

[Flags]
public enum DDS_SURFACE_FLAGS : uint
{
    DDS_SURFACE_FLAGS_TEXTURE = 0x00001000, // DDSCAPS_TEXTURE
    DDS_SURFACE_FLAGS_MIPMAP = 0x00400008, // DDSCAPS_COMPLEX | DDSCAPS_MIPMAP
    DDS_SURFACE_FLAGS_CUBEMAP = 0x00000008, // DDSCAPS_COMPLEX
}

[Flags]
public enum DDS_CUBEMAP_FLAGS : uint
{
    POSITIVEX = 0x00000600, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEX
    NEGATIVEX = 0x00000a00, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEX
    POSITIVEY = 0x00001200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEY
    NEGATIVEY = 0x00002200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEY
    POSITIVEZ = 0x00004200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEZ
    NEGATIVEZ = 0x00008200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEZ
    ALLFACES = (POSITIVEX | NEGATIVEX | POSITIVEY | NEGATIVEY | POSITIVEZ | NEGATIVEZ)
}

[Flags]
public enum DDS_RESV1_FLAGS : uint
{
    DDS_RESF1_NORMALMAP = 0x01000000,
    DDS_RESF1_DSDT = 0x02000000,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct DdsHeader
{
#pragma warning disable CS0169 // Field is never used
    public struct DdsReserved
    {
        private uint _field0;
        private uint _field1;
        private uint _field2;
        private uint _field3;
        private uint _field4;
        private uint _field5;
        private uint _field6;
        private uint _field7;
        private uint _field8;
        private uint _field9;
        private uint _fielda;
    }
#pragma warning restore CS0169 // Field is never used

    public struct ColorFloat
    {
        public float R;
        public float G;
        public float B;
        public float A;
    }

    public uint Size;
    public DDS_HEADER_FLAGS Flags;
    public uint Height;
    public uint Width;
    public uint PitchOrLinearSize;
    public uint Depth;
    public uint MipMapCount;
    public uint AlphaBitDepth;
    public DDS_RESV1_FLAGS Reserved1;
    public float AvgBrightness;
    public ColorFloat MinColor;
    public ColorFloat MaxColor;
    public DdsPixelFormat PixelFormat;
    public DDS_SURFACE_FLAGS SurfaceFlags;
    public DDS_CUBEMAP_FLAGS CubemapFlags;
    public byte PersistentMips;
    public byte TileMode;
    public byte Reserved2_0;
    public byte Reserved2_1;
    public byte Reserved2_2;
    public byte Reserved2_3;
    public byte Reserved2_4;
    public byte Reserved2_5;
    public uint TextureStage;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct DdsHeaderDxt10
{
    public DXGI_FORMAT DxgiFormat;
    public ResourceDimension ResourceDimension;
    public uint MiscFlag;
    public uint ArraySize;
    public uint MiscFlags2;
}