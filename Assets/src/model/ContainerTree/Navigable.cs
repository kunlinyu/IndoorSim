public enum Navigable
{
    PhysicallyNonNavigable = 0,
    LogicallyNonNavigable = 1,
    Navigable = 2,
    Mix = 3,
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