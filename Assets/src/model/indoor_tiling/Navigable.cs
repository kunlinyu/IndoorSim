using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Navigable
{
    PhysicallyNonNavigable,
    PhysicallyNavigableLogicallyNonNavigable,
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
// | PhyNaviLogNonNav | PhyNaviLogNonNav | PhyNaviLogNonNav    |
// | PhyNaviLogNonNav | Navigable        | PhyNaviLogNonNav    |
// | Navigable        | Navigable        | Navigable           |