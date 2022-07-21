using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PGM2Texture
{
    static public Texture2D Translate(PGMImage pgm)
    {
        Texture2D texture = new Texture2D(pgm.width(), pgm.height());
        for (int i = 0; i < pgm.width(); i++)
            for (int j = 0; j < pgm.height(); j++)
            {
                float color = (float)pgm.GetPixel(i, j) / pgm.colorMaximumValue();
                texture.SetPixel(i, j, new Color(color, color, color));
            }
        texture.Apply();
        return texture;
    }
}
