using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ToolType
{
    ViewSelect,
    RedoUndo,
    BasicShape,
    ParametricShape,
    DoAfterSelect,
    Asset,
}

[CreateAssetMenu]
public class ToolButtonData : ScriptableObject
{
    public string m_ToolName;
    public ToolType m_Class;
    public Sprite m_PortraitImage;
    public int m_SortingOrder;

}