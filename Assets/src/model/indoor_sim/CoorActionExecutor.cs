using System;
#nullable enable

public class IdCoorActionExecutor : AbstractActionExecutor
{
    double position_x;
    double position_y;
    object positionMutex = new object();

    public IdCoorActionExecutor(IAgentHW hw) : base(hw)
    {
        hw.RegisterSensorDataListener(SensorDataListener);
    }

    private void SensorDataListener(ISensorData sensorData)
    {
        Position position = sensorData as Position ?? throw new Exception("need position data");
        lock (positionMutex)
        {
            position_x = position.x;
            position_y = position.y;
        }
    }


    public override void Update()
    {
        lock (goalMutex)
        {
            if (goal == null) return;

            if (goal.type == ActionType.MoveToCoor)
            {
                if (pause)
                {
                    hw.SetControlCommand(new SpeedVec().Zero());
                }
                else
                {
                    ActionMoveToCoor action2Coor = goal as ActionMoveToCoor ?? throw new ArgumentException("action type mismatch");

                    double dx, dy;
                    lock (positionMutex)
                    {
                        dx = action2Coor.x - position_x;
                        dy = action2Coor.y - position_y;
                    }

                    bool reach = MoveToRelativeCoor(dx, dy, out double[] speed);
                    hw.SetControlCommand(new SpeedVec() { x = speed[0], y = speed[1] });

                    if (reach)
                    {
                        hw.SetControlCommand(new SpeedVec().Zero());
                        OnFinish?.Invoke(goal, null);

                        goal = null;
                        OnFinish = null;
                        OnGiveUp = null;
                    }
                }
            }
            else if (goal.type == ActionType.MoveToId)
            {


            }
            else
            {
                throw new ArgumentException("IdCoorAgent action type mismatch");
            }
        }
    }

    private bool MoveToRelativeCoor(double dx, double dy, out double[] speed)
    {
        double distance = Math.Sqrt(dx * dx + dy * dy);
        double speedNormal = distance > 1.0 ? 1.0 : (distance + 1.0f) / 2.0f;

        speed = new double[] { dx / distance * speedNormal, dy / distance * speedNormal };
        bool reach = distance < 0.01d;

        if (reach)
            speed = new double[] { 0.0d, 0.0d };
        return reach;
    }

}
