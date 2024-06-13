namespace StarBreaker.Forge;

public enum DataType : ushort
{
    Reference = 0x310,
    WeakPointer = 0x0210,
    StrongPointer = 0x0110,
    Class = 0x0010,
    EnumChoice = 0x000F,
    Guid = 0x000E,
    Locale = 0x000D,
    Double = 0x000C,
    Single = 0x000B,
    String = 0x000A,
    UInt64 = 0x0009,
    UInt32 = 0x0008,
    UInt16 = 0x0007,
    Byte = 0x0006,
    Int64 = 0x0005,
    Int32 = 0x0004,
    Int16 = 0x0003,
    SByte = 0x0002,
    Boolean = 0x0001
}