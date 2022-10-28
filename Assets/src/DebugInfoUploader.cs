using System;
using System.Security.Cryptography;

using UnityEngine;

using Newtonsoft.Json;

struct DebugInfo
{
    public PlatformInfo platformInfo;
    public SoftwareInfo softwareInfo;
    public EncriptionInfo encriptionInfo;
    public string content;
}

public class DebugInfoUploader : MonoBehaviour
{
    static public void DebugInfo(byte[] content, byte[] key, bool force)
    {
        DebugInfo debugInfo = new()
        {
            platformInfo = PlatformInfo.Get(),
            softwareInfo = SoftwareInfo.Get(),
        };


        Aes aes = Aes.Create();
        aes.Key = key;
        debugInfo.encriptionInfo = EncriptionInfo.Get(aes);

        byte[] encryptedData = AesEncryption.AesEncryptBase64(content, aes);
        Debug.Log("Length of encrypted zipped json: " + encryptedData.Length);
        aes.Dispose();

        debugInfo.content = Convert.ToBase64String(encryptedData);

        Debug.Log(JsonConvert.SerializeObject(debugInfo, Formatting.None));
    }
}
