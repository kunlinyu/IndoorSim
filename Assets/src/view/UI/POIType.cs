using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class POIType : ScriptableObject
{
    public new string name;
    public bool multiRelated;
    public bool relatedNavigable;
    public bool relatesToCurrent;
    public bool needDirection;
}
