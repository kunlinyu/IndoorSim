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
    List<CellSpace> connectedNodes = new List<CellSpace>();
    TopoExpressionValueType typeOfTopoExpression;
    string comment;
    List<ThematicLayer> connectedLayers = new List<ThematicLayer>();
}
