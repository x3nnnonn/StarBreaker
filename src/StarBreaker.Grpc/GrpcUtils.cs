using System.Buffers.Binary;

namespace StarBreaker.Grpc;

public static class GrpcUtils
{
    /// <summary>
    ///     Attempts to remove the gRPC header from a byte array,
    ///     allowing the data to be read by a protobuf parser.
    /// </summary>
    /// <param name="grpcData">The gRPC data to convert.</param>
    /// <returns>The data without the gRPC header.</returns>
    public static ReadOnlySpan<byte> GrpcToProtobuf(ReadOnlySpan<byte> grpcData)
    {
        if (grpcData.Length < 5)
            return grpcData;

        if (grpcData[0] != 0) 
            return grpcData;
        
        var length = BinaryPrimitives.ReadInt32BigEndian(grpcData[1..]);
        if (length > grpcData.Length - 5)
            return grpcData;

        return grpcData.Slice(5, length);
    }
}