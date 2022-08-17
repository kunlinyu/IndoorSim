using System.Collections.Generic;
using NetTopologySuite.Geometries;

#nullable enable

public class HumanPOI : IndoorPOI
{
    public HumanPOI(Point point, ICollection<Container> spaces) : base("human", spaces)
    {
        this.point = point;
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
