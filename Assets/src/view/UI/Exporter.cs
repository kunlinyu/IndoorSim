using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Exporter : ScriptableObject
{
    public new string name;
    public string defaultStreamName;
    public bool canIncludeFull;
}
