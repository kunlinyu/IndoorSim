using System;

// https://opengeospatial.github.io/poi/spec/poi-core.html
namespace poi {

public class POIProperties
{
    public string id;
    public Uri href;
    public string value;
    public Uri Base;
    public string type;
    public string lang;
    public DateTime created;
    public DateTime updated;
    public DateTime deleted;
    public POIProperties author;
    public POIProperties rights;
    public string term;
    public Uri scheme;
}

}
