using System;
using System.Collections.Generic;

public class GridMap
{
    // mutable
    public string id;
    public MapOrigin globalOrigin;


    // public int width;  // the user should parse imageBase64 to get width
    // public int height;  // the user should parse imageBase64 to get height

    // immutable
    public GridMapImageFormat format;
    public string zippedBase64Image;

    // immutable after loading
    public double resolution;  // set by user during importing
    public MapOrigin localOrigin;  // set by user during importing

    public GridMap Clone()
    {
        return new GridMap() {
            id = id,
            globalOrigin = globalOrigin.Clone(),
            // width = width,
            // height = height,
            format = format,
            zippedBase64Image = (string)zippedBase64Image.Clone(),
            resolution = resolution,
            localOrigin = localOrigin.Clone(),
        };
    }
}


public struct MapOrigin
{
    public double x;
    public double y;
    public double theta;

    public MapOrigin Clone() => new MapOrigin() { x = x, y = y, theta = theta };
}

public enum GridMapImageFormat
{
    PGM,
    PNG,
}