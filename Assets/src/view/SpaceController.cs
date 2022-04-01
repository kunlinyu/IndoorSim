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

    private GameObject polygonRenderObj;

    public float Distance(Vector3 vec)
    => (float)space.Geom.Distance(new GeometryFactory().CreatePoint(Utils.Vec2Coor(vec)));

    // Start is called before the first frame update
    void Start()
    {
        // transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        material = new Material(Shader.Find("Sprites/Default"));
        material.color = new Color(0.2f, 0.2f, 1.0f);
        updateRenderer();
    }

    // Update is called once per frame
    void Update()
    {
        if (needUpdateRenderer)
            updateRenderer();

    }

    void updateRenderer()
    {
        Destroy(polygonRenderObj);

        polygonRenderObj = new GameObject("polygon render obj");
        polygonRenderObj.transform.SetParent(transform);
        PolygonRenderer pr = polygonRenderObj.AddComponent<PolygonRenderer>();
        pr.enableBorder = false;
        pr.interiorMaterial = new Material(Shader.Find("Sprites/Default"));
        if (highLight)
            pr.interiorMaterial.color = new Color(0.5f, 0.5f, 1.0f, 0.3f);
        else
            pr.interiorMaterial.color = new Color(0.2f, 0.2f, 1.0f, 0.3f);
        pr.triangulationMaterial = new Material(Shader.Find("Sprites/Default"));
        pr.triangulationMaterial.color = new Color(1.0f, 1.0f, 1.0f);


        // pr.sortingLayerId = ;
        pr.sortingOrder = 0;

        Mesh mesh = pr.UpdatePolygon(space.Geom);
        Mesh triMesh = new Mesh();
        triMesh.Clear();
        triMesh.subMeshCount = 1;
        triMesh.SetVertices(mesh.vertices);
        triMesh.SetIndices(mesh.GetIndices(0), MeshTopology.Triangles, 0);
        GetComponent<MeshCollider>().sharedMesh = triMesh;

        needUpdateRenderer = false;
    }
}
