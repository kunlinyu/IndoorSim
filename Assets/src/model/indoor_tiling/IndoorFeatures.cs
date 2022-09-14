using System;
using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

using Newtonsoft.Json;
using System.Runtime.Serialization;

#nullable enable

public class IndoorFeatures
{
    public List<ThematicLayer> layers = new List<ThematicLayer>();
    // TODO: check there should be only one layer with same ThemeLayerValueType
    // TODO: use ThemeLayerValueType to get one layer and its accessor (IndoorTiling is one accessor of Topographic layer)
    public List<InterLayerConnection> layerConnections = null;

    [JsonIgnore] public ThematicLayer activeLayer = null;

    [JsonIgnore] public Action<ThematicLayer> OnLayerCreated = (layer) => { };
    [JsonIgnore] public Action<ThematicLayer> OnLayerRemoved = (layer) => { };

    [JsonIgnore] public Action<InterLayerConnection> OnLayerConnectionCreated = (layerConnection) => { };
    [JsonIgnore] public Action<InterLayerConnection> OnLayerConnectionRemoved = (layerConnection) => { };

    [JsonIgnore] public Action<ThematicLayer> OnActiveLayerSwitch = (layer) => { };

    public IndoorFeatures()
    {
    }

    [OnDeserialized]
    private void OnSerializedMethod(StreamingContext context)
    {
        if (layers.Count > 0)
            activeLayer = layers[0];
        else
            activeLayer = null;
    }

    public IndoorFeatures(string defaultLevelName)
    {
        layers.Add(new ThematicLayer(defaultLevelName));
        activeLayer = layers[0];
        OnLayerCreated?.Invoke(layers[0]);
    }

    public bool Contains(CellVertex vertex) => activeLayer.Contains(vertex);
    public bool Contains(CellBoundary boundary) => activeLayer.Contains(boundary);
    public bool Contains(CellSpace space) => activeLayer.Contains(space);
    public bool Contains(RLineGroup rLines) => activeLayer.Contains(rLines);
    public bool Contains(IndoorPOI poi) => activeLayer.Contains(poi);

    public CellVertex? FindVertexCoor(Point point) => activeLayer.FindVertexCoor(point);

    public CellVertex? FindVertexCoor(Coordinate coor) => activeLayer.FindVertexCoor(coor);
    public ICollection<CellBoundary> VertexPair2Boundaries(CellVertex cv1, CellVertex cv2) => activeLayer.VertexPair2Boundaries(cv1, cv2);
    public CellBoundary? FindBoundaryGeom(LineString ls) => activeLayer.FindBoundaryGeom(ls);

    public CellSpace? FindSpaceGeom(Coordinate coor) => activeLayer.FindSpaceGeom(coor);

    public CellSpace? FindContainerId(string id) => activeLayer.FindContainerId(id);

    public CellSpace? FindSpaceId(string id) => activeLayer.FindSpaceId(id);

    public IndoorPOI? FindIndoorPOI(Coordinate coor) => activeLayer.FindIndoorPOI(coor);

    public RepresentativeLine? FindRLine(LineString ls, out RLineGroup? rLineGroup) => activeLayer.FindRLine(ls, out rLineGroup);


    public string CalcDigest()
    {
        return string.Join(',', layers.Select(layer => layer.CalcDigest()));
    }
}
