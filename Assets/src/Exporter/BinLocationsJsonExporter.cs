using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using UnityEngine;
using Newtonsoft.Json;

class QuotesHelper
{
    public string IndoorSim;
}

public class BinLocationsJsonExporter : IExporter
{
    private static string idPrefix = "LacationYamlExporterId: ";
    public string name => "binlocations.json";
    public string defaultStreamName => "binlocations.json";
    public bool canIncludeFull => true;

    IndoorSimData indoorSimData = null;

    Dictionary<IndoorPOI, List<Container>> poi2Container = null;

    public void Load(IndoorSimData indoorSimData)
    {
        this.indoorSimData = indoorSimData;
    }

    public bool Translate(string layerName)
    {
        ThematicLayer layer = indoorSimData.indoorFeatures.layers.Find(layer => layer.level == layerName);
        if (layer == null) throw new ArgumentException("can not find layer with level name: " + layerName);

        poi2Container = new Dictionary<IndoorPOI, List<Container>>();
        layer.poiMember.ForEach(poi =>
        {
            if (!poi.LabelContains("PICKING")) return;
            poi2Container[poi] = new List<Container>();
            poi.foi.ForEach(foi => foi.AllNodeInContainerTree().ForEach(container => poi2Container[poi].Add(container)));
        });
        return true;
    }

    public string Export(string softwareVersion, bool includeFull)
    {
        if (poi2Container == null) throw new InvalidOperationException("Translate first");

        StringBuilder sb = new StringBuilder();
        sb.Append("{\n");

        HashSet<string> containerIds = new HashSet<string>();

        foreach (var entry in poi2Container)
        {
            if (entry.Key.CategoryContains(POICategory.Human.ToString())) continue;
            string poiIdFull = entry.Key.GetLabels().Find(label => label.StartsWith(idPrefix));
            if (poiIdFull == null)
            {
                Debug.Log(string.Join(',', entry.Key.GetLabels()));
                throw new InvalidOperationException("you should export locations.yaml first to generate if for POI");
            }
            string poiId = poiIdFull.Substring(idPrefix.Length);
            entry.Value.ForEach(container =>
            {
                if (container.containerId != "")
                {
                    if (containerIds.Contains(container.containerId)) throw new ArgumentException("redundent container id: " + container.containerId);
                    containerIds.Add(container.containerId);
                    sb.Append($"  \"{container.containerId}\": \"{poiId}\",\n");
                }
            });
        }

        // TODO: we should check if some id don't connect to picking point

        if (includeFull)
        {
            var quotesHelper = new QuotesHelper() { IndoorSim = indoorSimData.Serialize(softwareVersion, false) };
            sb.Append($"  \"IndoorSim\": {JsonConvert.SerializeObject(quotesHelper.IndoorSim)}\n");
        }


        sb.Append("}\n");

        return sb.ToString();
    }

    public void Export(Stream stream)
    {
        throw new System.NotImplementedException();
    }
}
