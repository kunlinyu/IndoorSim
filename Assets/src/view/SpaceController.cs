using System.Linq;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class SpaceController : MonoBehaviour, Selectable
{
    private CellSpace space;
    public CellSpace Space
    {
        get => space;
        set
        {
            space = value;
            space.OnUpdate += () => { ReTriangulate(); updateRenderer(); };
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
    public SelectableType type { get => SelectableType.Space; }
    [SerializeField] public Material material;
    [SerializeField] public Material highLightMaterial;
    [SerializeField] public Material selectedMaterial;
    [SerializeField] public Material triangulationMaterial;

    private GameObject polygonRenderObj;

    public float Distance(Vector3 vec)
    => (float)space.Geom.Distance(new GeometryFactory().CreatePoint(Utils.Vec2Coor(vec)));

    // Start is called before the first frame update
    void Start()
    {
        polygonRenderObj = new GameObject("polygon render obj");
        polygonRenderObj.transform.SetParent(transform);
        polygonRenderObj.AddComponent<PolygonRenderer>();

        ReTriangulate();
        updateRenderer();
    }

    // Update is called once per frame
    void Update()
    {
        if (needUpdateRenderer)
            updateRenderer();
    }

    void ReTriangulate()
    {
        PolygonRenderer pr = polygonRenderObj.GetComponent<PolygonRenderer>();
        Mesh mesh = pr.UpdatePolygon(space.Geom);
        Debug.Log("tri");
        Mesh triMesh = new Mesh();
        triMesh.Clear();
        triMesh.subMeshCount = 1;
        triMesh.SetVertices(mesh.vertices);
        triMesh.SetIndices(mesh.GetIndices(0), MeshTopology.Triangles, 0);
        GetComponent<MeshCollider>().sharedMesh = triMesh;
    }


    void updateRenderer()
    {
        PolygonRenderer pr = polygonRenderObj.GetComponent<PolygonRenderer>();
        pr.enableBorder = false;
        if (selected)
            pr.interiorMaterial = selectedMaterial;
        else if (highLight)
            pr.interiorMaterial = highLightMaterial;
        else
            pr.interiorMaterial = material;

        pr.triangulationMaterial = triangulationMaterial;

        pr.sortingOrder = 0;

        pr.UpdateRenderer();

        needUpdateRenderer = false;
    }
}
