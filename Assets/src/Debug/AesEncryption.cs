using System;
using System.IO;
using System.Security.Cryptography;

public class AesEncryption
{
    static public byte[] AesEncryptBase64(byte[] data, Aes aes)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            CryptoStream cryptoStream = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);


            BinaryWriter encryptWriter = new BinaryWriter(cryptoStream);
            encryptWriter.Write(data);
            encryptWriter.Flush();

            cryptoStream.FlushFinalBlock();

            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);

            var result = new byte[ms.Length];
            ms.Read(result, 0, (int)ms.Length);

            return result;
        }
    }
}
