namespace StarBreaker.P4k;

public sealed class ZipExtraField
{
    public ushort Tag { get; }
    public ushort Size { get; }
    public byte[] Data { get; }

    public ZipExtraField(ushort tag, ushort size, byte[] data)
    {
        Tag = tag;
        Size = size;
        Data = data;
    }
}