using System.Collections.Generic;
using NetTopologySuite.Geometries;

#nullable enable

public class PaAmrPoi : IndoorPOI
{
    public PaAmrPoi(Point amrPoint, ICollection<Container> spaces) : base("PaAmr", spaces)
    {
        this.point = amrPoint;
    }

    public static bool CanLayOnStatic(Container? container)
        => container != null && container.navigable == Navigable.Navigable;

    public static bool AcceptContainerStatic(Container? container)
        => container != null && container.navigable != Navigable.Navigable;

    public override bool CanLayOn(Container? container)
        => CanLayOnStatic(container);

    public override bool AcceptContainer(Container? container)
        => AcceptContainerStatic(container);
}
