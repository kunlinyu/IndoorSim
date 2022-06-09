public enum ActuatorCommandType
{
    SpeedVec,
    Twist2,
}

public interface IActuatorCommand
{
    public ActuatorCommandType type();
    public IActuatorCommand Zero();
}

public class SpeedVec : IActuatorCommand
{
    public double x;
    public double y;

    public ActuatorCommandType type() => ActuatorCommandType.SpeedVec;

    public IActuatorCommand Zero() => new SpeedVec() { x = 0.0d, y = 0.0d };
}

public class Twist2 : IActuatorCommand
{
    public double v_x;
    public double omega_z;
    public ActuatorCommandType type() => ActuatorCommandType.Twist2;
    public IActuatorCommand Zero() => new Twist2() { v_x = 0.0d, omega_z = 0.0d };
}