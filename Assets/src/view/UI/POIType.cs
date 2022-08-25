using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class POIType : ScriptableObject
{
    public new string name;
    public bool multiRelated;
    public bool relatedNavigable;
    public bool needDirection;
    public bool relatesToCurrent;
    public bool needQueue;
    public Color color;
}
