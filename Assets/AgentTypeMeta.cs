using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MovingMode
{
    Move2D,
    TwoWheel,
    AnyTwist,
}

[CreateAssetMenu]
public class AgentTypeMeta : ScriptableObject
{
    public string typeName = "";
    public float collisionRadius;
    public float height;
    public MovingMode MovingMode;
}