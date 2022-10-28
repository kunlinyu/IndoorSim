using System;
using System.Security.Cryptography;

public class EncriptionInfo
{
    public string Algorithm;
    public int KeySize;
    public string IV;
    public string Mode;
    public string Padding;

    static public EncriptionInfo Get(Aes aes)
    {
        return new EncriptionInfo()
        {
            Algorithm = "AES",
            KeySize = aes.KeySize,
            IV = BitConverter.ToString(aes.IV).Replace("-", " "),
            Mode = aes.Mode.ToString(),
            Padding = aes.Padding.ToString(),
        };
    }
}
