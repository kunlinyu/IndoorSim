using System.Collections;
using System.Collections.Generic;

enum TopoExpressionValueType
{
    Contains,
    Overlaps,
    Equals,
    Within,
    Crosses,
    Other,
}

public class InterLayerConnection
{
    TopoExpressionValueType typeOfTopoExpression;
    string comment;
    List<ThematicLayer> connectedLayers = new List<ThematicLayer>();
    List<CellSpace> connectedNodes = new List<CellSpace>();
}
