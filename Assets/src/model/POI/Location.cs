// https://opengeospatial.github.io/poi/spec/poi-core.html
namespace poi
{

    public class Location : POIProperties
    {
        public Geometry point = new Geometry();
        public Geometry line = null;
        public Geometry polygon = null;
        public POIProperties address;
        public Relationship relationship;
        public object undetermined;
    }

}
