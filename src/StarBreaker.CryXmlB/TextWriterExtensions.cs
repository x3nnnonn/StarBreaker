using System.Buffers;
using System.Text;

namespace StarBreaker.CryXmlB;

public static class TextWriterExtensions
{
    private static readonly SearchValues<char> _escapeCharacters = SearchValues.Create(['<', '>', '&', '\'', '"']);
    
    public static int WriteXmlString(this TextWriter writer, ReadOnlySpan<byte> data, int offset)
    {
        var relevantData = data[offset..];
        var length = relevantData.IndexOf((byte)'\0');

        if (length == 0)
            return length;

        Span<char> span = stackalloc char[length];
        Encoding.ASCII.GetChars(relevantData[..length], span);
        
        var escapeCount = span.IndexOfAny(_escapeCharacters);
        if (escapeCount == -1)
        {
            //nothing we need to escape, happy path. just write the span.
            writer.Write(span);
            return length;
        }
        
        //we have to escape some characters. don't worry about performance too much here
        var str = span.ToString();
        var replaced = str
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("'", "&apos;")
            .Replace("\"", "&quot;");
        
        writer.Write(replaced);

        return replaced.Length;
    }
}