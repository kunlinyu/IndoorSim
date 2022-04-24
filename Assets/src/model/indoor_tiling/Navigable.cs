using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Navigable
{
    PhysicallyNonNavigable,
    LogicallyNonNavigable,
    Navigable,
}

public enum NaviDirection
{
    NoneDirection,
    Left2Right,
    Right2Left,
    BiDirection,
}

// Boundary left and right definition
//      P1
//      ^
//      |
// left | right
//      |
//      P0

// | left-cellspace   | right cellspace  | boundary            |
// | -----------------+------------------+---------------------|
// | PhyNonNavi       |    *             | PhyNonNavi          |
// | LogNonNav        | LogNonNav        | LogNonNav           |
// | LogNonNav        | Navigable        | LogNonNav           |
// | Navigable        | Navigable        | Navigable           |