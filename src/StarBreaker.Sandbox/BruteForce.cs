using System.Globalization;
using System.Numerics;

namespace StarBreaker.Sandbox;

internal static class BruteForce
{
    public static byte[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890_-/\\"u8.ToArray();
    public const int MAX_LENGTH = 8;
    public const int CORES = 16;

    public static uint[] _test = null!;

    public static void Run()
    {
        _test = ReadKeys("keys.txt");

        var tasks = Enumerable.Range(0, CORES).Select(i => Task.Run(() => Brute(i)));

        Task.WaitAll(tasks);
    }

    public static void Brute(int i)
    {
        // Each core handles different starting characters for better distribution
        int charsPerCore = chars.Length / CORES;
        int startIndex = i * charsPerCore;
        int endIndex = (i == CORES - 1) ? chars.Length : (i + 1) * charsPerCore;

        // Try all lengths up to MAX_LENGTH
        for (int len = 1; len <= MAX_LENGTH; len++)
        {
            BruteLength(len, startIndex, endIndex);
        }
    }

    private static void BruteLength(int length, int startIndex, int endIndex)
    {
        Span<byte> current = stackalloc byte[length];

        // Initialize the hasher with first character
        for (int firstChar = startIndex; firstChar < endIndex; firstChar++)
        {
            current[0] = chars[firstChar];
            var hasher = new PartHasher(current[..1]);

            if (length == 1)
            {
                uint hash = hasher.Value;
                if (Array.BinarySearch(_test, hash) >= 0)
                {
                    Console.WriteLine($"Found: {System.Text.Encoding.ASCII.GetString(current)}");
                }
                continue;
            }

            BruteLengthRecursive(current, 1, hasher);
        }
    }

    private static void BruteLengthRecursive(Span<byte> current, int position, PartHasher hasher)
    {
        // Try all possible characters at current position
        foreach (byte c in chars)
        {
            current[position] = c;

            // Calculate hash with new character
            uint tempHash = hasher.WithChar(c);

            // If we're at the last position, check if hash exists
            if (position == current.Length - 1)
            {
                if (Array.BinarySearch(_test, tempHash) >= 0)
                {
                    Console.WriteLine($"Found: {System.Text.Encoding.ASCII.GetString(current)}");
                }
                continue;
            }

            // For intermediate positions, create new hasher with added character and recurse
            var newHasher = hasher;
            newHasher.Add(c);
            BruteLengthRecursive(current, position + 1, newHasher);
        }
    }

    static uint[] ReadKeys(string file)
    {
        var lines = File.ReadAllLines(file);
        var keys = new List<uint>();

        foreach (var line in lines)
        {
            if (line.StartsWith("0x") &&
                uint.TryParse(line[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var key))
            {
                keys.Add(key);
            }
        }

        return keys.Distinct().Order().ToArray();
    }
}

public struct PartHasher
{
    private uint Current;

    public PartHasher()
    {
        Current = 0xFFFFFFFFu;
    }

    public PartHasher(ReadOnlySpan<byte> chars)
    {
        Current = 0xFFFFFFFFu;

        foreach (var ch in chars)
            Current = BitOperations.Crc32C(Current, ch);
    }

    public readonly uint WithChar(byte c) => ~BitOperations.Crc32C(Current, c);

    public readonly uint WithChar(ReadOnlySpan<byte> chars) => WithChar(chars[0]);

    public void Add(byte c)
    {
        Current = BitOperations.Crc32C(Current, c);
    }

    public void Add(ReadOnlySpan<byte> chars)
    {
        Add(chars[0]);
    }

    public readonly uint Value => ~Current;
}


