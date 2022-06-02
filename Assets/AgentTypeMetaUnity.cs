using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class AgentTypeMetaUnity : ScriptableObject
{
    public string typeName = "";
    public float collisionRadius;
    public float height;
    public MovingMode MovingMode;

    AgentTypeMeta ToNoneUnity()
    {
        return new AgentTypeMeta() {
            typeName = typeName,
            collisionRadius = collisionRadius,
            height = height,
            MovingMode = MovingMode
        };
    }
}