using System;

public enum MovingMode
{
    Move2D,
    Twist2,
    TwoWheel,
    AnyTwist,
}

public class AgentTypeMeta
{
    public string typeName = "";
    public float collisionRadius;
    public float height;
    public MovingMode movingMode;

    public override bool Equals(object obj)
    {
        return obj is AgentTypeMeta meta &&
               typeName == meta.typeName &&
               collisionRadius == meta.collisionRadius &&
               height == meta.height &&
               movingMode == meta.movingMode;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(typeName, collisionRadius, height, movingMode);
    }
}