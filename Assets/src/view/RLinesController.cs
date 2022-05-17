using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;

public class RLineController : MonoBehaviour, Selectable
{
    public RepresentativeLine rLine;
    public RLineGroup rLines;
    public CellBoundary fr;
    public CellBoundary to;

    private bool _highLight = false;
    private bool needUpdateRenderer = true;
    public bool highLight
    {
        get => _highLight;
        set
        {
            _highLight = value;
            needUpdateRenderer = true;
        }
    }
    private bool _selected = false;
    public bool selected
    {
        get => _selected;
        set
        {
            _selected = value;
            needUpdateRenderer = true;
        }
    }

    public Material material;
    public Material materialDark;
    public Material materialHighlight;

    public SelectableType type { get => SelectableType.RLine; }

    public float Distance(Vector3 vec)
        => (float)rLine.geom.Distance(new GeometryFactory().CreatePoint(Utils.Vec2Coor(vec)));

    public string Tip()
        => $"from: {rLine.fr.Id}\n" +
           $"to: {rLine.to.Id}\n" +
           $"passType: {rLine.pass}";

    public float scrollSpeed = 1.0f;
    void Update()
    {

        if (highLight)
            GetComponent<LineRenderer>().material = materialHighlight;
        else if (rLine.pass == PassType.AllowedToPass)
            GetComponent<LineRenderer>().material = material;
        else if (rLine.pass == PassType.DoNotPass)
            GetComponent<LineRenderer>().material = materialDark;
        else
            throw new System.Exception("unknown pass type: " + rLine.pass);

        if (rLine.pass == PassType.AllowedToPass)
        {
            float offset = Time.time * -1.0f * scrollSpeed;
            GetComponent<LineRenderer>().material.SetTextureOffset("_MainTex", new Vector2(offset, 0));
        }
    }
}

public class RLinesController : MonoBehaviour
{
    private RLineGroup rLines;
    public RLineGroup RLines
    {
        get => rLines;
        set
        {
            rLines = value;
            rLines.OnUpdate += updateRenderer;
        }
    }
    private List<GameObject> renderObj = new List<GameObject>();

    public Material material;
    public Material materialDark;
    public Material materialHighlight;
    public float width = 0.05f;

    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        updateRenderer();
    }


    void updateRenderer()
    {
        renderObj.ForEach(obj => Destroy(obj));
        renderObj.Clear();

        foreach (var rLine in rLines.rLines)
        {
            if (rLine.IllForm(rLines.space)) continue;
            GameObject obj = new GameObject("rLine renderer");
            obj.transform.SetParent(transform);
            obj.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            obj.transform.position = Utils.Coor2Vec(rLine.geom.GetPointN(rLine.geom.NumPoints / 2).Coordinate);
            renderObj.Add(obj);

            var rlc = obj.AddComponent<RLineController>();
            rlc.rLines = rLines;
            rlc.rLine = rLine;
            rlc.fr = rLine.fr;
            rlc.to = rLine.to;
            rlc.material = material;
            rlc.materialDark = materialDark;
            rlc.materialHighlight = materialHighlight;

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.positionCount = rLine.geom.NumPoints;
            lr.SetPositions(rLine.geom.Coordinates.Select(coor => Utils.Coor2Vec(coor)).ToArray());
            lr.alignment = LineAlignment.TransformZ;
            lr.textureMode = LineTextureMode.Tile;
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.numCapVertices = 5;
            lr.numCornerVertices = 0;
            lr.sortingOrder = 2;

            SphereCollider sc = obj.AddComponent<SphereCollider>();
            sc.center = Vector3.zero;
            sc.radius = 0.1f;
        }

    }
}
