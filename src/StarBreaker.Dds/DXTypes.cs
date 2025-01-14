using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DirectXTexNet;

namespace StarBreaker.Dds;

[Flags]
public enum DdsPixelFormatFlags : uint
{
    AlphaPixels = 1,
    Alpha = 2,
    Fourcc = 4,
    Rgb = 64, // 0x00000040
    Yuv = 512, // 0x00000200
    Luminance = 131072, // 0x00020000
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
public enum DDSHEADER_FLAGS : uint
{
    CAPS = 1,
    HEIGHT = 2,
    WIDTH = 4,
    PITCH = 8,
    PIXELFORMAT = 0x1000, // 0x00001000
    MIPMAPCOUNT = 0x20000, // 0x00020000
    LINEARSIZE = 0x80000, // 0x00080000
    DEPTH = 0x800000, // 0x00800000
}

[Flags]
public enum  DDSHEADER_CAPS : uint
{
    COMPLEX = 8,
    MIPMAP = 0x400000,
    TEXTURE = 0x1000,
}

[Flags]
public enum DDSHEADER_CAPS2 : uint
{
    CUBEMAP = 0x200,
    CUBEMAP_POSITIVEX = 0x400,
    CUBEMAP_NEGATIVEX = 0x800,
    CUBEMAP_POSITIVEY = 0x1000,
    CUBEMAP_NEGATIVEY = 0x2000,
    CUBEMAP_POSITIVEZ = 0x4000,
    CUBEMAP_NEGATIVEZ = 0x8000,
    VOLUME = 0x200000,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct DdsHeader
{
    [InlineArray(11)]
    public struct DdsReserved
    {
        private uint _field;
    }

    public uint Size;
    public DDSHEADER_FLAGS Flags;
    public uint Height;
    public uint Width;
    public uint PitchOrLinearSize;
    public uint Depth;
    public uint MipMapCount;
    public DdsReserved Reserved1;
    public DdsPixelFormat PixelFormat;
    public DDSHEADER_CAPS Caps;
    public DDSHEADER_CAPS2 Caps2;
    public uint Caps3;
    public uint Caps4;
    public uint Reserved2;
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