using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(SpriteRenderer))]
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
    public SelectableType type { get => SelectableType.Boundary; }

    [SerializeField] public Material material;
    [SerializeField] public Material highLightMaterial;
    [SerializeField] public Material selectedMaterial;

    public float widthFactor = 0.01f;
    private float width = 0.0f;

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
        lr.sortingOrder = 2;

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

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (Boundary.Navigable() == Navigable.Navigable)
        {
            switch (Boundary.NaviDirection)
            {
                case NaviDirection.NoneDirection:
                    sr.sprite = null;
                    break;
                case NaviDirection.Left2Right:
                    sr.sprite = Resources.Load<Sprite>("BoundaryDirection/left2right");
                    break;
                case NaviDirection.Right2Left:
                    sr.sprite = Resources.Load<Sprite>("BoundaryDirection/right2left");
                    break;
                case NaviDirection.BiDirection:
                    sr.sprite = Resources.Load<Sprite>("BoundaryDirection/bi-direction");
                    break;
            }
        }
        else
        {
            sr.sprite = null;
        }
        sr.sortingOrder = 3;
        sr.drawMode = SpriteDrawMode.Sliced;

        float arrowSizeFactor = 0.2f;
        float maxSpriteSize = 0.2f;
        float spriteSize = Mathf.Min((float)boundary.Geom.Length * arrowSizeFactor, maxSpriteSize);

        sr.size = new Vector2(spriteSize, spriteSize);

        needUpdateRenderer = false;
    }

    public string Tip()
        => $"Id: {boundary.Id}\n" +
           $"P0: {boundary.P0.Id}\n" +
           $"P1: {boundary.P1.Id}\n" +
           $"left:  {boundary.leftSpace?.Id}\n" +
           $"right: {boundary.rightSpace?.Id}\n" +
           $"navigable: {boundary.Navigable()}";

}
