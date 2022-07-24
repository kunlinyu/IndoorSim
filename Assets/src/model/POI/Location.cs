// https://opengeospatial.github.io/poi/spec/poi-core.html
namespace poi {

public class Location : POIProperties
{
    public Geometry point;
    public Geometry line;
    public Geometry polygon;
    public POIProperties address;
    public Relationship relationship;
    public object undetermined;
}

}
