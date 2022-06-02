public enum MovingMode
{
    Move2D,
    TwoWheel,
    AnyTwist,
}

public class AgentTypeMeta
{
    public string typeName = "";
    public float collisionRadius;
    public float height;
    public MovingMode MovingMode;
}