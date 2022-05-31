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
    public IControlCommand Zero();
}

public class SpeedVec : IControlCommand
{
    public double x;
    public double y;

    public ControlCommandType type() => ControlCommandType.SpeedVec;

    public IControlCommand Zero() => new SpeedVec() { x = 0.0d, y = 0.0d };
}

public class Twist : IControlCommand
{
    public double v_x;
    public double omega_z;
    public ControlCommandType type() => ControlCommandType.Twist;
    public IControlCommand Zero() => new Twist() { v_x = 0.0d, omega_z = 0.0d };
}