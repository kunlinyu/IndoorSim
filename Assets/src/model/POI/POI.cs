using System.Collections.Generic;

// https://opengeospatial.github.io/poi/spec/poi-core.html
namespace poi {

public class POI : POIProperties
{
    public List<POIProperties> label = new List<POIProperties>();
    public List<POIProperties> description = new List<POIProperties>();
    public List<POIProperties> category = new List<POIProperties>();
    public List<POIProperties> time = new List<POIProperties>();
    public List<POIProperties> link = new List<POIProperties>();
    public Location location;
    public List<POIProperties> metadata = new List<POIProperties>();
}

}
