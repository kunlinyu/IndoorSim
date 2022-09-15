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
            boundary.OnUpdate += () => { updateRenderer(boundary.geom.Coordinates.Select(coor => U.Coor2Vec(coor)).ToArray()); };
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
    [SerializeField] public Material logNonNaviMat;
    [SerializeField] public Material phyNonNaviMat;
    [SerializeField] public Material highLightMaterial;
    [SerializeField] public Material selectedMaterial;
    [SerializeField] public float edgeShrink = 0.1f;

    public float widthFactor = 0.01f;
    private float width = 0.0f;

    private int lastCameraHeightInt;

    public float Distance(Vector3 vec)
    => (float)boundary.geom.Distance(new GeometryFactory().CreatePoint(U.Vec2Coor(vec)));

    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        updateRenderer(boundary.geom.Coordinates.Select(coor => U.Coor2Vec(coor)).ToArray());
        updateCollider();
        updateTransform();
    }

    // Update is called once per frame
    void Update()
    {
        int newHeightInt = (int)(CameraController.CameraPosition.y * 0.5f);
        if (lastCameraHeightInt != newHeightInt)
        {
            lastCameraHeightInt = newHeightInt;
            needUpdateRenderer = true;
            width = newHeightInt * 2.0f * widthFactor + 0.01f;
        }
        if (needUpdateRenderer)
            updateRenderer(boundary.geom.Coordinates.Select(coor => U.Coor2Vec(coor)).ToArray());
    }

    void updateCollider()
    {
        GetComponent<BoxCollider>().center = Vector3.zero;
        GetComponent<BoxCollider>().size = new Vector3((float)boundary.geom.Length, 0.1f, 0.1f);
    }

    void updateTransform()
    {
        transform.localPosition = U.Coor2Vec(boundary.geom.Centroid.Coordinate);

        Coordinate start = boundary.geom.StartPoint.Coordinate;
        Coordinate end = boundary.geom.EndPoint.Coordinate;
        float x = (float)(start.X - end.X);
        float y = (float)(start.Y - end.Y);
        float theta = Mathf.Atan2(y, x);

        transform.rotation = Quaternion.Euler(90.0f, 0.0f, theta * Mathf.Rad2Deg);
    }

    public void updateRenderer(Vector3[] positions)
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.positionCount = boundary.geom.NumPoints;
        lr.SetPositions(positions);
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = material;

        // TODO: do not assign material if nothing changed
        if (selected)
            lr.material = selectedMaterial;
        else if (highLight)
        {
            lr.material = highLightMaterial;
            lr.startWidth = width * 2.0f;
            lr.endWidth = width * 2.0f;
        }
        else if (boundary.Navigable == Navigable.Navigable)
            lr.material = material;
        else if (boundary.Navigable == Navigable.LogicallyNonNavigable)
            lr.material = logNonNaviMat;
        else if (boundary.Navigable == Navigable.PhysicallyNonNavigable)
            lr.material = phyNonNaviMat;
        else
            throw new System.Exception("unknown navigable enum: " + boundary.Navigable);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (Boundary.SmartNavigable() == Navigable.Navigable)
        {
            switch (Boundary.NaviDir)
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

        float arrowSizeFactor = 0.2f;
        float maxSpriteSize = 0.2f;
        float spriteSize = Mathf.Min((float)boundary.geom.Length * arrowSizeFactor, maxSpriteSize);

        sr.size = new Vector2(spriteSize, spriteSize);


        UpdateEdge();

        needUpdateRenderer = false;
    }

    private void UpdateEdge()
    {
        LineRenderer lr = transform.Find("Edge").GetComponent<LineRenderer>();

        // TODO: we should reconsider the if statement and put it in another place
        if (boundary.NaviDir != NaviDirection.NoneDirection &&
            boundary.leftSpace != null && boundary.leftSpace.navigable == Navigable.Navigable &&
            boundary.rightSpace != null && boundary.rightSpace.navigable == Navigable.Navigable)
        {
            lr.enabled = true;
            lr.positionCount = 2;
            Vector3 left = U.Coor2Vec(boundary.leftSpace.Geom.Centroid.Coordinate);
            Vector3 right = U.Coor2Vec(boundary.rightSpace.Geom.Centroid.Coordinate);
            lr.SetPosition(0, (left - right).normalized * edgeShrink + right);
            lr.SetPosition(1, (right - left).normalized * edgeShrink + left);
        }
        else
        {
            lr.enabled = false;
        }
    }

    public string Tip()
        => $"Id: {boundary.Id}\n" +
           $"P0: {boundary.P0.Id}\n" +
           $"P1: {boundary.P1.Id}\n" +
           $"left:  {boundary.leftSpace?.Id}\n" +
           $"right: {boundary.rightSpace?.Id}\n" +
           $"navigable: {boundary.Navigable}";

}
