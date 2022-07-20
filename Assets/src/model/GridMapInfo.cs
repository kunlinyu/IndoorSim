using System.Collections;
using System.Collections.Generic;

public struct GridMapInfo
{
    public string id;
    public int width;
    public int height;
    public double resolution;
    public MapOrigin localOrigin;
    public MapOrigin globalOrigin;
    public GridMapImageFormat format;
    public string imageBase64;
}


public struct MapOrigin
{
    double x;
    double y;
    double theta;
}

public enum GridMapImageFormat
{
    PGM,
    PNG,
}