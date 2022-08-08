using System;

// https://opengeospatial.github.io/poi/spec/poi-core.html
namespace poi {

public class POIProperties
{
    public string id;    // A unique identifier for this location. Can be a URI fragment.
    public Uri href;     // absolute reference when id and Base do not combine to form a deferenceable URL
    public string value;
    public Uri Base;     // https://www.w3.org/TR/2009/REC-xmlbase-20090128/ base url when id is not absolute URL
    public string type;  // https://www.ietf.org/rfc/rfc2046.txt e.g. text, image, audio, video, application, multipart, message
    public string lang;  // https://www.ietf.org/rfc/rfc3066.txt e.g. en-US, en-GB, en-AU, zh-CN
    public DateTime created;
    public DateTime updated;
    public DateTime deleted;
    public POIProperties author;
    public POIProperties rights;
    public string term;  // A machine-readable character string to designate any number of discrete choices.
    public Uri scheme;   // An absolute reference to the schema enumerating the discrete choices in term.
}

}
