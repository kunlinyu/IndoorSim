using System.Collections;
using System.Collections.Generic;

public struct GridMap
{
    // mutable
    public string id;
    public MapOrigin globalOrigin;

    // immutable

    public int width;  // depends on imageBase64
    public int height;  // depends on imageBase64
    public GridMapImageFormat format;  // depends on imageBase64
    public string imageBase64;

    // immutable after loading

    public double resolution;  // set by user during loading
    public MapOrigin localOrigin;  // set by user during loading
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