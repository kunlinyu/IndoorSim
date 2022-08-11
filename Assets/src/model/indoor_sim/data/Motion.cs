#nullable enable

public enum MotionType
{
    Move,
}

public abstract class Motion
{
    public MotionType type;
    public double time;
}

public class MoveToCoorMotion : Motion
{
    public double x;
    public double y;
    public MoveToCoorMotion(double x, double y)
    {
        type = MotionType.Move;
        this.x = x;
        this.y = y;
    }
}