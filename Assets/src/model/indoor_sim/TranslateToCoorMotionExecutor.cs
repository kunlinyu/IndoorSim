using System;
#nullable enable

public class TranslateToCoorMotionExecutor : AbstractMotionExecutor
{
    double distance = Double.MaxValue;

    Position position = new Position();

    // we need grid map and A* and DWA

    public TranslateToCoorMotionExecutor(IActuatorSensor hw) : base(hw)
    { }

    protected override bool Finish() => distance <= 0.01d;
    protected override bool GiveUp() => false;

    protected override IActuatorCommand PauseCommand() => new SpeedVec() { x = 0.0f, y = 0.0f };
    protected override IActuatorCommand StopCommand() => new SpeedVec() { x = 0.0f, y = 0.0f };

    protected override IActuatorCommand SensorDataListener(ISensorData sensorData, Motion? goal)
    {
        position = sensorData as Position ?? throw new Exception("need position data");

        if (goal == null)
        {
            distance = Double.MaxValue;
            return StopCommand();
        }
        MoveToCoorMotion action2Coor = goal as MoveToCoorMotion ?? throw new ArgumentException("action type mismatch");

        double dx = action2Coor.x - position.x;
        double dy = action2Coor.y - position.y;

        distance = MoveToRelativeCoor(dx, dy, out double[] speed);
        return new SpeedVec() { x = speed[0], y = speed[1] };

    }


    private static double MoveToRelativeCoor(double dx, double dy, out double[] speed)
    {
        double distance = Math.Sqrt(dx * dx + dy * dy);
        double speedNormal = distance > 1.0 ? 1.0 : (distance + 1.0f) / 2.0f;

        speed = new double[] { dx / distance * speedNormal, dy / distance * speedNormal };
        return distance;
    }

    public Position Position() => position;

    public override bool Accept(Motion goal)
        => goal.type == MotionType.Move;
}
