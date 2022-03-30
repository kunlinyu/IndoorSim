using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SelectableType
{
    Vertex,
    Boundary,
    Space,
}
public interface Selectable
{
    SelectableType type { get; }

    bool highLight { set; get; }

    float Distance(Vector3 vec);

}
