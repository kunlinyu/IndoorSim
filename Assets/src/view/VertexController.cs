using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(SphereCollider))]
public class VertexController : MonoBehaviour, Selectable
{
    private CellVertex vertex;
    public CellVertex Vertex
    {
        get => vertex;
        set
        {
            vertex = value;
            vertex.OnUpdate += () => { updateRenderer(U.Coor2Vec(vertex.Coordinate)); };
            vertex.OnUpdate += updateCollider;
            vertex.OnUpdate += updateTransform;
        }
    }

    [SerializeField] public float widthFactor = 0.8f;
    [SerializeField] public float radiusFactor = 0.008f;
    [SerializeField] public int step = 16;
    [SerializeField] public int sortingLayerId = 0;
    [SerializeField] public int sortingOrder = 3;
    [SerializeField] public Material material;
    [SerializeField] public Material highLightMaterial;
    [SerializeField] public Material selectedMaterial;

    private float radius;
    private float width;

    private bool _highLight = false;
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

    private bool needUpdateRenderer = true;

    public SelectableType type { get => SelectableType.Vertex; }

    public float Distance(Vector3 vec)
        => (float)vertex.Coordinate.Distance(U.Vec2Coor(vec));

    private int lastCameraHeightInt;


    // Start is called before the first frame update
    void Start()
    {
        GetComponent<LineRenderer>().positionCount = 0;
        updateRenderer(U.Coor2Vec(vertex.Coordinate));
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
            radius = newHeightInt * 2.0f * radiusFactor + 0.01f;
            width = radius * widthFactor;
        }
        if (needUpdateRenderer)
            updateRenderer(U.Coor2Vec(vertex.Coordinate));
    }

    void updateCollider()
    {
        GetComponent<SphereCollider>().center = Vector3.zero;
        GetComponent<SphereCollider>().radius = 0.1f;
    }

    void updateTransform()
    {
        transform.localPosition = U.Coor2Vec(vertex.Coordinate);
        transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f); ;
    }

    private static Vector3[] CirclePosition(Vector3 center, float radius, int step)
    {
        Vector3[] result = new Vector3[step];
        for (int i = 0; i < step; i++)
        {
            float theta = 2 * Mathf.PI / step * i;
            float x = center.x + radius * Mathf.Cos(theta);
            float z = center.z + radius * Mathf.Sin(theta);
            result[i] = new Vector3(x, 0.0f, z);
        }
        return result;
    }

    public void updateRenderer(Vector3 position)
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.positionCount = step;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.SetPositions(CirclePosition(position, radius, step));
        lr.material = material;

        // TODO: do not assign material if nothing changed
        if (selected)
        {
            lr.material = selectedMaterial;
        }
        else if (highLight)
        {
            lr.SetPositions(CirclePosition(position, radius * 1.5f, step));  // TODO: maybe change scale is better
            lr.material = highLightMaterial;
            lr.startWidth = width * 1.5f;
            lr.endWidth = width * 1.5f;
        }

        needUpdateRenderer = false;
    }

    public string Tip() => vertex.Id + " " + vertex.Coordinate.X + " " + vertex.Coordinate.Y;

}
