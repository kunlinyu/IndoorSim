using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class AgentTypeMetaUnity : ScriptableObject
{
    public string typeName = "";
    public float collisionRadius;
    public float height;
    public MovingMode movingMode;

    public AgentTypeMetaUnity(AgentTypeMeta meta)
    {
        typeName = meta.typeName;
        collisionRadius = meta.collisionRadius;
        height = meta.height;
        movingMode = meta.movingMode;
    }

    public override bool Equals(object obj)
        => obj is AgentTypeMetaUnity another && ToNoneUnity().Equals(another.ToNoneUnity());

    public override int GetHashCode() => ToNoneUnity().GetHashCode();

    public AgentTypeMeta ToNoneUnity()
        => new AgentTypeMeta()
        {
            typeName = typeName,
            collisionRadius = collisionRadius,
            height = height,
            movingMode = movingMode
        };
}