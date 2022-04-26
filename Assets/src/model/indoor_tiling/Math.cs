using System;
using NetTopologySuite.Geometries;
#nullable enable
public class M
{
    static public Coordinate Rotate(Coordinate origin, double angleRad)
    {
        double cos = Math.Cos(angleRad);
        double sin = Math.Sin(angleRad);
        double x = origin.X;
        double y = origin.Y;
        return new Coordinate(cos * x - sin * y, sin * x + cos * y);
    }

    static public Coordinate Rotate(Coordinate origin, Coordinate anchor, double angleRad)
    {
        Coordinate move = new Coordinate(origin.X - anchor.X, origin.Y - anchor.Y);
        Coordinate rotated = Rotate(move, angleRad);
        return new Coordinate(rotated.X + anchor.X, rotated.Y + anchor.Y);
    }

    static public Coordinate Translate(Coordinate origin, Coordinate dirFrom, Coordinate dirTo, double distance)
    {
        double length = dirFrom.Distance(dirTo);
        return new Coordinate((dirTo.X - dirFrom.X) / length * distance + origin.X, (dirTo.Y - dirFrom.Y) / length * distance + origin.Y);
    }

    static public Coordinate[] bazierCurve3(Coordinate P0, Coordinate P1, Coordinate P2, int steps)
    {
        Coordinate[] result = new Coordinate[steps + 1];
        for (int i = 0; i < steps + 1; i++)
        {
            float t = (float)i / (float)steps;
            float s = (1.0f - t);
            float a0 = 1.0f * s * s;
            float a1 = 2.0f * s * t;
            float a2 = 1.0f * t * t;
            result[i] = new Coordinate(a0 * P0.X + a1 * P1.X + a2 * P2.X, a0 * P0.Y + a1 * P1.Y + a2 * P2.Y);
        }
        return result;
    }
    static public Coordinate[] bazierCurve4(Coordinate P0, Coordinate P1, Coordinate P2, Coordinate P3, int steps)
    {
        Coordinate[] result = new Coordinate[steps + 1];
        for (int i = 0; i < steps + 1; i++)
        {
            float t = (float)i / (float)steps;
            float s = (1.0f - t);
            float a0 = 1.0f * s * s * s;
            float a1 = 3.0f * s * s * t;
            float a2 = 3.0f * s * t * t;
            float a3 = 1.0f * t * t * t;
            result[i] = new Coordinate(a0 * P0.X + a1 * P1.X + a2 * P2.X + a3 * P3.X, a0 * P0.Y + a1 * P1.Y + a2 * P2.Y + a3 * P3.Y);
        }
        return result;
    }
}
