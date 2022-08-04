using UnityEngine;

public static class PGM2Texture
{
    static public bool Translate(Texture2D tex, PGMImage pgm)
    {
        bool ret = tex.Reinitialize(pgm.width(), pgm.height());
        if (!ret) return false;
        for (int i = 0; i < pgm.width(); i++)
            for (int j = 0; j < pgm.height(); j++)
            {
                float color = (float)pgm.GetPixel(i, j) / pgm.colorMaximumValue();
                tex.SetPixel(i, j, new Color(color, color, color));
            }
        tex.Apply();
        return tex;
    }
    public static bool LoadPGMImage(this Texture2D tex, byte[] data)
    {
        PGMImage pgm = new PGMImage();
        pgm.Load(data);
        PGM2Texture.Translate(tex, pgm);
        return true;
    }
}
