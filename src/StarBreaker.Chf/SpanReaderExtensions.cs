using StarBreaker.Common;

namespace StarBreaker.Chf;

public static class SpanReaderExtensions
{
    public static T ReadKeyValueAndChildCount<T>(this SpanReader reader, int count, uint key) where T : unmanaged
    {
        reader.Expect(key);
        var data = reader.Read<T>();
        reader.Expect(count);
        
        return data;
    }
}