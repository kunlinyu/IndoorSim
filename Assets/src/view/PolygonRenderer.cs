using NetTopologySuite.Geometries;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PolygonRenderer : MonoBehaviour
{
    public Material interiorMaterial;
    public Material triangulationMaterial;
    public Material boundaryMaterial;

    public float boundaryWidth;
    public int sortingLayerId;
    public int sortingOrder;

    public bool enableBorder;

    private GameObject lineRendererObj;

    void Start()
    {
    }

    void Update()
    {

    }

    public void UpdateRenderer()
    {
        GetComponent<MeshRenderer>().materials = new Material[] { interiorMaterial, triangulationMaterial };
    }

    public Mesh UpdateMesh(Mesh mesh)
    {
        GetComponent<MeshFilter>().mesh = mesh;

        return mesh;
    }

    public Mesh UpdateMeshBorder(Mesh mesh, Polygon polygon)
    {
        GetComponent<MeshFilter>().mesh = mesh;

        if (enableBorder)
            UpdateBorder(polygon);
        else
            Destroy(lineRendererObj);

        return mesh;
    }

    public Mesh UpdatePolygon(Polygon polygon)
    {
        GetComponent<MeshFilter>().mesh = U.TriangulatePolygon2Mesh(polygon);

        if (enableBorder)
            UpdateBorder(polygon);
        else
            Destroy(lineRendererObj);

        return GetComponent<MeshFilter>().mesh;
    }

    private void UpdateBorder(Polygon polygon)
    {
        Destroy(lineRendererObj);
        lineRendererObj = new GameObject("line renderer for polygon boundary");
        lineRendererObj.transform.SetParent(transform);
        lineRendererObj.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        LineRenderer lr = lineRendererObj.AddComponent<LineRenderer>();
        lr.positionCount = polygon.Coordinates.Length;
        for (int i = 0; i < polygon.Coordinates.Length; i++)
        {
            lr.SetPosition(i, U.Coor2Vec(polygon.Coordinates[i]));
        }

        lr.alignment = LineAlignment.TransformZ;
        lr.useWorldSpace = true;

        lr.loop = true;
        lr.startWidth = boundaryWidth;
        lr.endWidth = boundaryWidth;
        lr.numCapVertices = 3;
        lr.numCornerVertices = 3;

        lr.material = boundaryMaterial;
    }
}
