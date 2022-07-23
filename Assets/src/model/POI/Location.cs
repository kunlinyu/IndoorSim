// https://opengeospatial.github.io/poi/spec/poi-core.html
public class Location : POIProperties
{
    Geometry point;
    Geometry line;
    Geometry polygon;
    POIProperties address;
    Relationship relationship;
    object undetermined;
}
