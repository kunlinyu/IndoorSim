using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(BoxCollider))]
public class BoundaryController : MonoBehaviour, Selectable
{
    private CellBoundary boundary;
    public CellBoundary Boundary
    {
        get => boundary;
        set
        {
            boundary = value;
            boundary.OnUpdate += () => { updateRenderer(boundary.Geom.Coordinates.Select(coor => Utils.Coor2Vec(coor)).ToArray()); };
            boundary.OnUpdate += updateCollider;
            boundary.OnUpdate += updateTransform;
        }
    }

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

    [SerializeField] public Material material;
    [SerializeField] public Material highLightMaterial;
    [SerializeField] public Material selectedMaterial;

    public float widthFactor = 0.01f;
    private float width = 0.0f;
    public SelectableType type { get => SelectableType.Boundary; }

    private int lastCameraHeightInt;

    public float Distance(Vector3 vec)
    => (float)boundary.Geom.Distance(new GeometryFactory().CreatePoint(Utils.Vec2Coor(vec)));

    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        updateRenderer(boundary.Geom.Coordinates.Select(coor => Utils.Coor2Vec(coor)).ToArray());
        updateCollider();
        updateTransform();
    }

    // Update is called once per frame
    void Update()
    {
        int newHeightInt = (int)(Camera.main.transform.position.y * 0.5f);
        if (lastCameraHeightInt != newHeightInt)
        {
            lastCameraHeightInt = newHeightInt;
            needUpdateRenderer = true;
            width = newHeightInt * 2.0f * widthFactor + 0.01f;
        }
        if (needUpdateRenderer)
            updateRenderer(boundary.Geom.Coordinates.Select(coor => Utils.Coor2Vec(coor)).ToArray());
    }

    void updateCollider()
    {
        GetComponent<BoxCollider>().center = Vector3.zero;
        GetComponent<BoxCollider>().size = new Vector3((float)boundary.Geom.Length, 0.1f, 0.1f);
    }

    void updateTransform()
    {
        transform.localPosition = Utils.Coor2Vec(boundary.Geom.Centroid.Coordinate);

        Coordinate start = boundary.Geom.StartPoint.Coordinate;
        Coordinate end = boundary.Geom.EndPoint.Coordinate;
        float x = (float)(start.X - end.X);
        float y = (float)(start.Y - end.Y);
        float theta = Mathf.Atan2(y, x);

        transform.rotation = Quaternion.Euler(90.0f, 0.0f, theta * Mathf.Rad2Deg);
    }

    public void updateRenderer(Vector3[] positions)
    {
        // TODO use prefab
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.positionCount = boundary.Geom.NumPoints;
        lr.SetPositions(positions);
        lr.alignment = LineAlignment.TransformZ;
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = 5;
        lr.numCornerVertices = 0;
        lr.material = material;
        lr.sortingOrder = 1;

        if (selected)
        {
            lr.material = selectedMaterial;
        }
        else if (highLight)
        {
            lr.material = highLightMaterial;
            lr.startWidth = width * 2.0f;
            lr.endWidth = width * 2.0f;
        }

        needUpdateRenderer = false;
    }
}
