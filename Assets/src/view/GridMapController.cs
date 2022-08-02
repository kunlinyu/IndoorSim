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
            Texture2D texture;
            int width;
            int height;
            byte[] imageBytes = Decompress(Convert.FromBase64String(gridMap.zippedBase64Image));
            if (gridMap.format == GridMapImageFormat.PGM)
            {
                PGMImage pgm = new PGMImage();
                pgm.Load(imageBytes);
                texture = PGM2Texture.Translate(pgm);
                width = pgm.width();
                height = pgm.height();
            }
            else if (gridMap.format == GridMapImageFormat.PNG)
            {
                texture = new Texture2D(1, 1);
                texture.LoadImage(imageBytes);
                width = texture.width;
                height = texture.height;
            }
            else
                throw new Exception("unrecognize gridmap format: " + gridMap.format);

            Sprite sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, width, height), new Vector2(0f, 0f), (float)(1.0d / value.resolution));
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
