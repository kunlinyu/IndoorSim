using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SensorDataType
{
    Position,
}
public interface ISensorData
{
    public SensorDataType type();
}

public class Position : ISensorData
{
    public SensorDataType type() => SensorDataType.Position;
    public double x;
    public double y;
}
