using System;
#nullable enable

public class IdCoorActionExecutor : AgentHWActionExecutor
{
    double distance = Double.MaxValue;

    MapService mapService;

    public IdCoorActionExecutor(IAgentHW hw, MapService mapService) : base(hw)
    {
        this.mapService = mapService;
    }

    protected override bool Finish() => distance <= 0.01d;
    protected override bool GiveUp() => false;

    protected override IControlCommand PauseCommand() => new SpeedVec() { x = 0.0f, y = 0.0f };
    protected override IControlCommand StopCommand() => new SpeedVec() { x = 0.0f, y = 0.0f };

    protected override IControlCommand SensorDataListener(ISensorData sensorData, AgentAction? goal)
    {
        if (goal == null) return StopCommand();

        Position position = sensorData as Position ?? throw new Exception("need position data");

        if (goal.type == ActionType.MoveToCoor)
        {
            ActionMoveToCoor action2Coor = goal as ActionMoveToCoor ?? throw new ArgumentException("action type mismatch");

            double dx = action2Coor.x - position.x;
            double dy = action2Coor.y - position.y;

            distance = MoveToRelativeCoor(dx, dy, out double[] speed);
            return new SpeedVec() { x = speed[0], y = speed[1] };
        }
        else if (goal.type == ActionType.MoveToId)
        {
            return StopCommand();
        }
        else
        {
            throw new ArgumentException("IdCoorAgent action type mismatch: " + goal.type);
        }
    }


    private static double MoveToRelativeCoor(double dx, double dy, out double[] speed)
    {
        double distance = Math.Sqrt(dx * dx + dy * dy);
        double speedNormal = distance > 1.0 ? 1.0 : (distance + 1.0f) / 2.0f;

        speed = new double[] { dx / distance * speedNormal, dy / distance * speedNormal };
        return distance;
    }

}
