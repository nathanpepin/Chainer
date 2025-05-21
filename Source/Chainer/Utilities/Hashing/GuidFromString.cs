using System.Security.Cryptography;
using System.Text;

namespace Chainer.Utilities.Hashing;

/// <summary>
/// Provides utilities for generating GUIDs deterministically from strings.
/// </summary>
public static class GuidFromString
{
    /// <summary>
    /// Creates a deterministic GUID based on a given string value.
    /// </summary>
    /// <param name="input">The string input used to generate the deterministic GUID.</param>
    /// <returns>A GUID that is deterministically generated from the provided input string.</returns>
    public static Guid CreateDeterministicGuid(string input)
    {
        // Use DNS namespace as default namespace
        var namespaceGuid = new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        using var algorithm = SHA1.Create();
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

    /// <summary>
    /// Reorders the bytes of a GUID to conform to the UUID variant layout.
    /// This operation is necessary for ensuring the proper endianness
    /// in deterministic GUID generation scenarios.
    /// </summary>
    /// <param name="guid">
    /// A byte array representing a GUID in its current byte order.
    /// The method modifies this array in place to reflect the swapped byte order.
    /// </param>
    private static void SwapByteOrder(byte[] guid)
    {
        // Swap the byte order to match UUID variant layout
        SwapBytes(guid, 0, 3);
        SwapBytes(guid, 1, 2);
        SwapBytes(guid, 4, 5);
        SwapBytes(guid, 6, 7);
    }

    /// <summary>
    /// Swaps the positions of two specified bytes in an array.
    /// </summary>
    /// <param name="guid">The byte array whose bytes will be swapped.</param>
    /// <param name="left">The index of the first byte to swap.</param>
    /// <param name="right">The index of the second byte to swap.</param>
    private static void SwapBytes(byte[] guid, int left, int right)
    {
        (guid[left], guid[right]) = (guid[right], guid[left]);
    }
}