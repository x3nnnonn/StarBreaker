namespace StarBreaker.Common;

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
        if (grpcData.Length < 5 || grpcData[0] != 0)
            throw new ArgumentException("Invalid gRPC data");
        
        // The first byte is the compression flag, which we don't care about.
        // The next four bytes are the length of the protobuf message.
        
        //hopefully just discarding them works.
        
        return grpcData[5..];
    }
}