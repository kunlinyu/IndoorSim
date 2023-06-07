using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

using UnityEngine;
using UnityEngine.Networking;

using Newtonsoft.Json;

struct DebugInfo
{
    public PlatformInfo platformInfo;
    public SoftwareInfo softwareInfo;
    public EncriptionInfo encryptionInfo;
    public string content;
}

class DataWrapHelper
{
    public Func<string> dataGetter;
    public string mapId;
    public string latestUpdateTime;
}

public class DebugInfoUploader : MonoBehaviour
{
    private readonly ConcurrentQueue<DataWrapHelper> dataGetterQueue = new ConcurrentQueue<DataWrapHelper>();
    public byte[] Key { set; get; }
    public string uriPrefix;

    public void Append(Func<string> dataGetter, string mapId, string latestUpdateTime)
    {
        var dataWrap = new DataWrapHelper() {  dataGetter = dataGetter, mapId = mapId, latestUpdateTime = latestUpdateTime };
        dataGetterQueue.Enqueue(dataWrap);
    }
    
    private void Update()
    {
        //StartCoroutine(GetCompressEncriptBase64PackUpload());
    }

    private IEnumerator GetCompressEncriptBase64PackUpload()
    {
        // Get data Getter
        if (!dataGetterQueue.TryDequeue(out var dataWrap)) yield break;

        yield return null;  // Don't get data immediatly. Make sure don't block current frame

        // Get data
        var data = dataWrap.dataGetter?.Invoke();
        yield return null;

        // Compress
        var zippedText = Compress(Encoding.ASCII.GetBytes(data));
        yield return null;

        // Encript
        Aes aes = Aes.Create();
        aes.Key = Key;
        byte[] encryptedData = AesEncryption.AesEncryptBase64(zippedText, aes);
        yield return null;

        // Base64
        var base64 = Convert.ToBase64String(encryptedData);
        yield return null;

        // Pack
        DebugInfo debugInfo = new()
        {
            platformInfo = PlatformInfo.Get(),
            softwareInfo = SoftwareInfo.Get(),
            encryptionInfo = EncriptionInfo.Get(aes),
            content = base64
        };
        aes.Dispose();
        var packedDebugInfo = JsonConvert.SerializeObject(debugInfo, Formatting.None);
        Debug.Log("debug packed debug into length: " + packedDebugInfo.Length);
        yield return null;

        // Upload
        yield return Upload(packedDebugInfo, dataWrap.mapId, dataWrap.latestUpdateTime);
    }

    private IEnumerator Upload(string packedDebugInfo, string mapId, string latestUpdateTime)
    {
        string uri = uriPrefix + $"map/{mapId}/{latestUpdateTime}";
        Debug.Log("upload debug info to " + uri);
        UnityWebRequest www = new(uri, "POST")
        {
            downloadHandler = new DownloadHandlerBuffer(),
            uploadHandler = new UploadHandlerRaw(Encoding.ASCII.GetBytes(packedDebugInfo)) { contentType = "application/json" }
        };

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            Debug.LogWarning($"{www.result}: {www.error}\n{www.downloadHandler.text}");
        else
            Debug.Log("Response: " + www.downloadHandler.text);
    }

    // TODO repeat code
    private static byte[] Decompress(byte[] bytes)
    {
        using (var memoryStream = new MemoryStream(bytes))
        using (var outputStream = new MemoryStream())
        {
            using (var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                decompressStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }

    // TODO repeat code
    private static byte[] Compress(byte[] bytes)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, System.IO.Compression.CompressionLevel.Optimal))
                gzipStream.Write(bytes, 0, bytes.Length);
            return memoryStream.ToArray();
        }
    }
}
