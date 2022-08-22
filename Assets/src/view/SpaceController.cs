using System;
using System.Linq;
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
            space.OnUpdate += ReTriangulateUpdateRenderer;
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

    // TODO: maybe we should remove material below
    [SerializeField] public Material material;
    [SerializeField] public Material logNonNaviMat;
    [SerializeField] public Material phyNonNaviMat;
    [SerializeField] public Material highLightMaterial;
    [SerializeField] public Material selectedMaterial;
    [SerializeField] public Material triangulationMaterial;

    public float Distance(Vector3 vec)
    => (float)space.Polygon.Distance(new GeometryFactory().CreatePoint(U.Vec2Coor(vec)));

    void Start()
    {
        ReTriangulateUpdateRenderer();
    }

    void Update()
    {
        if (needUpdateRenderer)
            updateRenderer(Vector3.zero);
    }

    void ReTriangulateUpdateRenderer()
    {
        ReTriangulate();
        updateRenderer(Vector3.zero);

        GameObject node = transform.Find("Node").gameObject;
        if (space.navigable == Navigable.Navigable)
        {
            node.GetComponent<SpriteRenderer>().enabled = true;
            node.transform.position = U.Coor2Vec(space.Geom.Centroid.Coordinate);
        }
        else
        {
            node.GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    void ReTriangulate()
    {
        PolygonRenderer pr = GetComponent<PolygonRenderer>();
        Mesh mesh = pr.UpdatePolygon(space.Polygon);
        Mesh triMesh = new Mesh();
        triMesh.Clear();
        triMesh.subMeshCount = 1;
        triMesh.SetVertices(mesh.vertices);
        triMesh.SetIndices(mesh.GetIndices(0), MeshTopology.Triangles, 0);
        GetComponent<MeshCollider>().sharedMesh = triMesh;
    }


    public void updateRenderer(Vector3 offset)
    {
        PolygonRenderer pr = GetComponent<PolygonRenderer>();
        transform.localPosition = offset;
        pr.enableBorder = false;
        if (selected)
            pr.interiorMaterial = selectedMaterial;
        else if (highLight)
            pr.interiorMaterial = highLightMaterial;
        else if (space.Navigable == Navigable.Navigable)
            pr.interiorMaterial = material;
        else if (space.Navigable == Navigable.LogicallyNonNavigable)
            pr.interiorMaterial = logNonNaviMat;
        else if (space.Navigable == Navigable.PhysicallyNonNavigable)
            pr.interiorMaterial = phyNonNaviMat;
        else
            throw new Exception("unknown navigable enum: " + space.Navigable);

        pr.triangulationMaterial = triangulationMaterial;

        pr.sortingOrder = 1;

        pr.UpdateRenderer();

        needUpdateRenderer = false;
    }

    public string DebugTip()
    {
        return $"Geom.Holes.Length: {space.Polygon.Holes.Length}\n" +
               $"Holes.Count: {space.Holes.Count}\n" +
               $"Geom.Shell.NumPoints: {space.Polygon.Shell.NumPoints}\n" +
               $"allBoundaries.Count: {space.allBoundaries.Count}\n" +
               $"Geom.Contains.InteriorPoint: {space.Geom.Contains(space.Geom.InteriorPoint)}";
    }

    public string Tip()
    {
        return DebugTip() + "\n" +
                $"id: {space.Id}\n" +
                $"navigable: {space.Navigable}\n" +
                $"container id: {space.containerId}\n" +
                $"children id: {string.Join(',', space.children.Select(child => child.containerId))}";
    }

    void OnDestroy()
    {
        space.OnUpdate -= ReTriangulateUpdateRenderer;
    }
}
