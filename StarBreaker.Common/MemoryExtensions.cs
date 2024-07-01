using System.Runtime.InteropServices;

namespace StarBreaker.Common;

public static class MemoryExtensions
{
    /// <summary>
    /// Trims the span to specified length if it exceeds it.
    /// If length is less that <paramref name="maxLength" /> then the original span returned.
    /// </summary>
    /// <typeparam name="T">The type of items in the span.</typeparam>
    /// <param name="span">A contiguous region of arbitrary memory.</param>
    /// <param name="maxLength">Maximum length.</param>
    /// <returns>Trimmed span.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxLength"/> is less than zero.</exception>
    public static Span<T> TrimLength<T>(this Span<T> span, int maxLength)
    {
        switch (maxLength)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            case 0:
                span = default;
                break;
            default:
                if (span.Length > maxLength)
                    span = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), maxLength);
                break;
        }

        return span;
    }

    /// <summary>
    /// Trims the memory block to specified length if it exceeds it.
    /// If length is less that <paramref name="maxLength" /> then the original block returned.
    /// </summary>
    /// <typeparam name="T">The type of items in the span.</typeparam>
    /// <param name="memory">A contiguous region of arbitrary memory.</param>
    /// <param name="maxLength">Maximum length.</param>
    /// <returns>Trimmed memory block.</returns>
    public static Memory<T> TrimLength<T>(this Memory<T> memory, int maxLength) => memory.Length <= maxLength ? memory : memory.Slice(0, maxLength);
}