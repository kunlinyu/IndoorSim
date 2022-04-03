using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ToolType
{
    RedoUndo,    // redo, undo
    SelectDrag,  // view, select, move
    BasicShape,  // line string
    ParametricShape,  // block...
    DoAfterSelect,  // remove
    Asset,  // save asset, load asset
}

[CreateAssetMenu]
public class ToolButtonData : ScriptableObject
{
    public string m_ToolName;
    public ToolType m_Class;
    public Sprite m_PortraitImage;
    public int m_SortingOrder;

}