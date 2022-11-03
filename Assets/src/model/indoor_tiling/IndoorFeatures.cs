using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.IO;

using NetTopologySuite.Geometries;

using Newtonsoft.Json;

#nullable enable

public class IndoorFeatures
{
    public List<ThematicLayer> layers = new List<ThematicLayer>();
    // TODO: check there should be only one layer with same ThemeLayerValueType
    // TODO: use ThemeLayerValueType to get one layer and its accessor (IndoorTiling is one accessor of Topographic layer)
    public List<InterLayerConnection>? layerConnections = null;

    [JsonIgnore] private ThematicLayer? activeLayer;
    [JsonIgnore] public ThematicLayer ActiveLayer { get => activeLayer ?? throw new Exception("activeLayer is null"); set => activeLayer = value; }
    public void ClearActiveLayer() => activeLayer = null;

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
            ActiveLayer = layers[0];
        else
            activeLayer = null;
    }

    public IndoorFeatures(string defaultLevelName)
    {
        layers.Add(new ThematicLayer(defaultLevelName));
        ActiveLayer = layers[0];
        OnLayerCreated?.Invoke(layers[0]);
    }

    public string Serialize(bool indent = false)
    {
        JsonSerializerSettings settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects,
            Formatting = indent ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>() { new WKTConverter(), new CoorConverter() },
            ContractResolver = ShouldSerializeContractResolver.Instance,
            DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffffK",
        };

        JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);

        StringBuilder sb = new StringBuilder(256);
        StringWriter sw = new StringWriter(sb);
        using (JsonTextWriter jsonWriter = new JsonTextWriter(sw))
        {
            jsonWriter.Formatting = jsonSerializer.Formatting;
            jsonWriter.IndentChar = '\t';
            jsonWriter.Indentation = 1;
            jsonSerializer.Serialize(jsonWriter, this, null);
        }

        return sw.ToString();  // return JsonConvert.SerializeObject(this);
    }

    public string SerializeIndexFast(bool indent = false)
    {
        StringBuilder sb = new StringBuilder(256);
        StringWriter sw = new StringWriter(sb);
        using (JsonTextWriter writer = new JsonTextWriter(sw))
        {
            writer.Formatting = indent ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None;
            writer.IndentChar = '\t';
            writer.Indentation = 1;

            writer.WriteStartObject();


            writer.WritePropertyName("layers");
            writer.WriteStartArray();
            foreach (var layer in layers)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("semanticExtension");
                writer.WriteValue(layer.semanticExtension);
                writer.WritePropertyName("theme");
                writer.WriteValue(layer.theme);
                writer.WritePropertyName("level");
                writer.WriteValue(layer.level);

                writer.WritePropertyName("cellVertexMember");
                writer.WriteStartArray();
                foreach (var vertex in layer.cellVertexMember)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("Id");
                    writer.WriteValue(vertex.Id);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WritePropertyName("cellBoundaryMember");
                writer.WriteStartArray();
                foreach (var boundary in layer.cellBoundaryMember)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("Id");
                    writer.WriteValue(boundary.Id);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WritePropertyName("cellSpaceMember");
                writer.WriteStartArray();
                foreach (var space in layer.cellSpaceMember)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("Id");
                    writer.WriteValue(space.Id);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        return sw.ToString();
    }
    public bool Contains(CellVertex vertex) => ActiveLayer.Contains(vertex);
    public bool Contains(CellBoundary boundary) => ActiveLayer.Contains(boundary);
    public bool Contains(CellSpace space) => ActiveLayer.Contains(space);
    public bool Contains(RLineGroup rLines) => ActiveLayer.Contains(rLines);
    public bool Contains(IndoorPOI poi) => ActiveLayer.Contains(poi);

    public CellVertex? FindVertexCoor(Point point) => ActiveLayer.FindVertexCoor(point);

    public CellVertex? FindVertexCoor(Coordinate coor) => ActiveLayer.FindVertexCoor(coor);
    public ICollection<CellBoundary> VertexPair2Boundaries(CellVertex cv1, CellVertex cv2) => ActiveLayer.VertexPair2Boundaries(cv1, cv2);
    public CellBoundary? FindBoundaryGeom(LineString ls) => ActiveLayer.FindBoundaryGeom(ls);

    public CellSpace? FindSpaceGeom(Coordinate coor) => ActiveLayer.FindSpaceGeom(coor);

    public CellSpace? FindContainerId(string id) => ActiveLayer.FindContainerId(id);

    public CellSpace? FindSpaceId(string id) => ActiveLayer.FindSpaceId(id);

    public IndoorPOI? FindIndoorPOI(Coordinate coor) => ActiveLayer.FindIndoorPOI(coor);

    public RepresentativeLine? FindRLine(LineString ls, out RLineGroup? rLineGroup) => ActiveLayer.FindRLine(ls, out rLineGroup);


    public string CalcDigest()
    {
        return string.Join(',', layers.Select(layer => layer.CalcDigest()));
    }
}
