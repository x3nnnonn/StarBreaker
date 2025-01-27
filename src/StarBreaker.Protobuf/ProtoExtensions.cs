using Google.Protobuf.Reflection;

namespace StarBreaker.Protobuf;

public static class ProtoExtensions
{
#pragma warning disable CS0618 // Type or member is obsolete
    public static bool IsProto3(this FileDescriptor fileDescriptor) => fileDescriptor.Syntax == Syntax.Proto3;
#pragma warning restore CS0618 // Type or member is obsolete
}