using System.Collections.Generic;
using System.Linq;

// https://opengeospatial.github.io/poi/spec/poi-core.html
namespace poi
{

    public class POI : POIProperties
    {
        public List<POIProperties> label = new List<POIProperties>();
        public List<POIProperties> description = new List<POIProperties>();
        public List<POIProperties> category = new List<POIProperties>();
        public List<POIProperties> time = new List<POIProperties>();
        public List<POIProperties> link = new List<POIProperties>();
        public Location location = new Location();
        public List<POIProperties> metadata = new List<POIProperties>();

        public void AddLabel(string value) => AddLabel(value, "en-US", "primary");

        /// <summary>
        /// Add one Label to this POI
        /// </summary>
        /// <param name="value">this is the value string of the label</param>
        /// <param name="lang">which language we use in the value string(https://www.ietf.org/rfc/rfc3066.txt). For example: en-US, en-GB, en-AU, zh-CN, </param>
        /// <param name="term">"primary" or "note"</param>
        public void AddLabel(string value, string lang, string term)
        {
            var label = new poi.POIProperties()
            {
                value = value,
                lang = lang,
                term = term,
            };

            this.label.Add(label);
        }

        public bool LabelContains(string value) => label.Any(label => label.value == value);

        public void AddDescription(string value) => AddDescription(value, "en-US");
        public void AddDescription(string value, string lang)
        {
            var description = new poi.POIProperties()
            {
                value = value,
                lang = lang,
            };

            this.description.Add(description);
        }


        public void AddCategory(string term) => AddCategory(term, "free", "");

        /// <summary>
        ///     Add Category
        /// </summary>
        /// <param name="term">keyword to specify the category</param>
        /// <param name="scheme">classification scheme to which the value belongs</param>
        /// <param name="value">describe human readable categorical</param>
        public void AddCategory(string term, string scheme, string value)
        {
            var category = new poi.POIProperties()
            {
                term = term,
                scheme = new System.Uri(scheme),
                value = value,
            };

            this.category.Add(category);
        }

        public void AddPointLocation(Geometry location) => this.location.point = location;
        public void AddLineLocation(Geometry location) => this.location.line = location;
        public void AddPolygonLocation(Geometry location) => this.location.polygon = location;

    }

}
