using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ControlCommandType
{
    SpeedVec,
    Twist,
}

public interface IControlCommand
{
    public ControlCommandType type();
}

public class SpeedVec : IControlCommand
{
    public double x;
    public double y;
    public ControlCommandType type() => ControlCommandType.SpeedVec;
}

public class Twist : IControlCommand
{
    public double v_x;
    public double omega_z;
    public ControlCommandType type() => ControlCommandType.Twist;
}