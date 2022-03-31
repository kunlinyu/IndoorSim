using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SpaceController : MonoBehaviour, Selectable
{
    private CellSpace space;
    public CellSpace Space
    {
        get => space;
        set
        {
            space = value;
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
    public SelectableType type { get => SelectableType.Space; }
    [SerializeField] public Material material;
    public float Distance(Vector3 vec)
    => (float)space.Geom.Distance(new GeometryFactory().CreatePoint(Utils.Vec2Coor(vec)));

    // Start is called before the first frame update
    void Start()
    {
        updateRenderer();
        transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        material = new Material(Shader.Find("Sprites/Default"));
        material.color = new Color(0.2f, 0.2f, 1.0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (needUpdateRenderer)
        {
            updateRenderer();
            needUpdateRenderer = false;
        }

    }

    void updateRenderer()
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.positionCount = space.Geom.ExteriorRing.NumPoints;
        lr.SetPositions(space.Geom.ExteriorRing.Coordinates.Select(coor => Utils.Coor2Vec(coor)).ToArray());
        lr.alignment = LineAlignment.TransformZ;
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;
        lr.material = material;
        lr.sortingOrder = 2;
    }
}
