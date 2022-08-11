
public enum SensorDataType
{
    Position,
    Pose2,
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

public class Pose2 : ISensorData
{
    public SensorDataType type() => SensorDataType.Pose2;
    public double x;
    public double y;
    public double theta;
}
