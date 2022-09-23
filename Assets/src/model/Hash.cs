using System.Text;
using System.Security.Cryptography;

public class Hash
{
    public static byte[] GetHash(string inputString)
    {
        using (HashAlgorithm algorithm = MD5.Create())
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
    }

    public static string GetHashString(string inputString)
    {
        StringBuilder sb = new StringBuilder();
        foreach (byte b in GetHash(inputString))
            sb.Append(b.ToString("X2"));

        return sb.ToString();
    }
}
