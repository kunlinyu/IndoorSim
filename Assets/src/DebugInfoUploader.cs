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
    int count = 0;
    static int kTriggerThresHold = 5;
    public void DebugInfo(byte[] content, byte[] key, bool force)
    {
        count++;
        if (count >= kTriggerThresHold) count = 0;
        if (count != 0) return;

        DebugInfo debugInfo = new()
        {
            platformInfo = PlatformInfo.Get(),
            softwareInfo = SoftwareInfo.Get(),
        };


        Aes aes = Aes.Create();
        aes.Key = key;
        debugInfo.encriptionInfo = EncriptionInfo.Get(aes);

        byte[] encryptedData = AesEncryption.AesEncryptBase64(content, aes);
        aes.Dispose();

        debugInfo.content = Convert.ToBase64String(encryptedData);

        var json = JsonConvert.SerializeObject(debugInfo, Formatting.None);
        Debug.Log("debuginfo.length: " + json.Length);

        // UnityWebRequest www = UnityWebRequest.Get("http://indoorsim-log-cn-dev.syriusdroids.com/spec");
        // www.SendWebRequest();
        // while (www.result == UnityWebRequest.Result.InProgress)
        //     Thread.Sleep(10);
        // if (www.result != UnityWebRequest.Result.Success)
        // {
        //     Debug.LogWarning(www.result);
        //     Debug.LogWarning(www.error);
        // }
        // else
        // {
        //     Debug.Log(www.downloadHandler.text);
        // }
    }
}
