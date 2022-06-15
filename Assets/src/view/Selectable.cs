using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SelectableType
{
    Vertex,
    Boundary,
    Space,
    RLine,
    Agent,
}
public interface Selectable
{
    SelectableType type { get; }

    bool highLight { set; get; }

    bool selected { set; get; }

    float Distance(Vector3 vec);

    string Tip();
}
