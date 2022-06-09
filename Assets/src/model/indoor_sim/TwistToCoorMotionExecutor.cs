using System;
using UnityEngine;
#nullable enable

public class TwistToCoorMotionExecutor : AbstractMotionExecutor
{
    double distance = Double.MaxValue;

    Pose2 pose = new Pose2();

    // we need grid map and A* and DWA

    public TwistToCoorMotionExecutor(IActuatorSensor hw) : base(hw)
    { }

    protected override bool Finish() => distance <= 0.02d;
    protected override bool GiveUp() => false;

    protected override IActuatorCommand PauseCommand() => new Twist2() { v_x = 0.0f, omega_z = 0.0f };
    protected override IActuatorCommand StopCommand() => new Twist2() { v_x = 0.0f, omega_z = 0.0f };

    protected override IActuatorCommand SensorDataListener(ISensorData sensorData, Motion? goal)
    {
        if (goal == null)
        {
            distance = Double.MaxValue;
            return StopCommand();
        }

        pose = sensorData as Pose2 ?? throw new Exception("need Pose2 data");
        MoveToCoorMotion action2Coor = goal as MoveToCoorMotion ?? throw new ArgumentException("action type mismatch");

        double dx = action2Coor.x - pose.x;
        double dy = action2Coor.y - pose.y;

        distance = Math.Sqrt(dx * dx + dy * dy);
        double speedNormal = distance > 1.0 ? 1.0 : (distance + 0.2f) / 1.2f;

        double dir = Math.Atan2(dy, dx);
        while (pose.theta > Math.PI) pose.theta -= 2 * Math.PI;
        while (pose.theta <-Math.PI) pose.theta += 2 * Math.PI;
        double dTheta = dir - pose.theta;

        if (dTheta > Math.PI) dTheta -= 2* Math.PI;
        if (dTheta <-Math.PI) dTheta += 2* Math.PI;

        Twist2 twist = new Twist2();


        if (Math.Abs(dTheta) < 0.05d)
            twist.v_x = speedNormal;
        else if (Math.Abs(dTheta) < 0.1d)
            twist.v_x = speedNormal * 0.6;
        else if (Math.Abs(dTheta) < 0.2d)
            twist.v_x = speedNormal * 0.2;
        else twist.v_x = 0.0d;

        twist.omega_z = dTheta * 0.5d;

        return twist;
    }

    public Pose2 Pose() => pose;

    public Position Position() => new Position() { x = pose.x, y = pose.y };

    public override bool Accept(Motion goal)
        => goal.type == MotionType.Move;
}
