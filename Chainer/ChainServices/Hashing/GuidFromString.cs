using System.Text;

namespace Chainer.ChainServices.Hashing;

internal static class GuidFromString
{
    public static Guid CreateDeterministicGuid(string input)
    {
        // Use DNS namespace as default namespace
        Guid namespaceGuid = new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        using var algorithm = System.Security.Cryptography.SHA1.Create();
        var namespaceBytes = namespaceGuid.ToByteArray();
        var inputBytes = Encoding.UTF8.GetBytes(input);

        // Adjust endianness for namespace
        SwapByteOrder(namespaceBytes);

        // Combine namespace and input
        var combinedBytes = new byte[namespaceBytes.Length + inputBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, combinedBytes, 0, namespaceBytes.Length);
        Buffer.BlockCopy(inputBytes, 0, combinedBytes, namespaceBytes.Length, inputBytes.Length);

        // Compute hash
        var hashBytes = algorithm.ComputeHash(combinedBytes);

        // Set version (5) and variant bits
        hashBytes[6] = (byte)((hashBytes[6] & 0x0F) | 0x50);
        hashBytes[8] = (byte)((hashBytes[8] & 0x3F) | 0x80);
        
        return new Guid(hashBytes[..16]);
    }

    private static void SwapByteOrder(byte[] guid)
    {
        // Swap the byte order to match UUID variant layout
        SwapBytes(guid, 0, 3);
        SwapBytes(guid, 1, 2);
        SwapBytes(guid, 4, 5);
        SwapBytes(guid, 6, 7);
    }

    private static void SwapBytes(byte[] guid, int left, int right)
    {
        (guid[left], guid[right]) = (guid[right], guid[left]);
    }
}