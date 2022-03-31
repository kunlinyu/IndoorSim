using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class BoundaryController : MonoBehaviour, Selectable
{
    private CellBoundary boundary;
    public CellBoundary Boundary
    {
        get => boundary;
        set
        {
            boundary = value;
            boundary.OnUpdate += updateRenderer;
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

    [SerializeField] public Material material;

    public float widthFactor = 0.02f;
    public SelectableType type { get => SelectableType.Boundary; }

    private int lastCameraHeightInt;

    public float Distance(Vector3 vec)
    => (float)boundary.Geom.Distance(new GeometryFactory().CreatePoint(Utils.Vec2Coor(vec)));

    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        material = new Material(Shader.Find("Sprites/Default"));
        material.color = new Color(1.0f, 0.5f, 1.0f);
        updateRenderer();
    }

    // Update is called once per frame
    void Update()
    {
        int newHeightInt = (int)(Camera.main.transform.position.y * 0.5f);
        if (lastCameraHeightInt != newHeightInt)
        {
            lastCameraHeightInt = newHeightInt;
            needUpdateRenderer = true;
        }
        if (needUpdateRenderer)
            updateRenderer();
    }

    void updateRenderer()
    {
        float width = Camera.main.transform.position.y * widthFactor;
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.positionCount = boundary.Geom.NumPoints;
        lr.SetPositions(boundary.Geom.Coordinates.Select(coor => Utils.Coor2Vec(coor)).ToArray());
        lr.alignment = LineAlignment.TransformZ;
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = 5;
        lr.numCornerVertices = 0;
        lr.material = material;
        lr.sortingOrder = 1;

        needUpdateRenderer = false;
    }
}
