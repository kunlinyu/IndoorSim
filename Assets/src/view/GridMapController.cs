using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public class GridMapController : MonoBehaviour
{
    private GridMap gridMap;
    public GridMap GridMap
    {
        get => gridMap;
        set
        {
            gridMap = value;
            Texture2D tex = new Texture2D(1, 1);
            byte[] imageBytes = Decompress(Convert.FromBase64String(gridMap.zippedBase64Image));
            if (gridMap.format == GridMapImageFormat.PGM)
                tex.LoadPGMImage(imageBytes);
            else if (gridMap.format == GridMapImageFormat.PNG)
                tex.LoadImage(imageBytes);
            else
                throw new Exception("unrecognize gridmap format: " + gridMap.format);

            Sprite sprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0f, 0f), (float)(1.0d / value.resolution));
            GetComponent<SpriteRenderer>().sprite = sprite;
            transform.localPosition = new Vector3((float)value.localOrigin.x, 0.0f, (float)value.localOrigin.y);
            transform.localRotation = Quaternion.Euler(90.0f, (float)value.localOrigin.theta / Mathf.PI * 180.0f * -1.0f, 0.0f);
        }
    }

    void Update()
    {

    }

    public static byte[] Decompress(byte[] bytes)
    {
        using (var memoryStream = new MemoryStream(bytes))
        using (var outputStream = new MemoryStream())
        {
            using (var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                decompressStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }
}
