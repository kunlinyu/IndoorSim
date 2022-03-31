using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
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
            vertex.OnUpdate += updateRenderer;
            vertex.OnUpdate += updateCollider;
            vertex.OnUpdate += updateTransform;
        }
    }

    [SerializeField] public float widthFactor = 0.5f;
    [SerializeField] public float radiusFactor = 0.01f;
    [SerializeField] public int step = 16;
    [SerializeField] public int sortingLayerId = 0;
    [SerializeField] public int sortingOrder = 3;
    [SerializeField] public Material material;

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

    public SelectableType type { get => SelectableType.Vertex; }

    public float Distance(Vector3 vec)
        => (float)vertex.Coordinate.Distance(Utils.Vec2Coor(vec));

    private int lastCameraHeightInt;


    // Start is called before the first frame update
    void Start()
    {
        GetComponent<LineRenderer>().positionCount = 0;
        material = new Material(Shader.Find("Sprites/Default"));
        material.color = new Color(1.0f, 0.8f, 0.8f);
        updateRenderer();
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
        }
        if (needUpdateRenderer)
            updateRenderer();
    }

    void updateCollider()
    {
        GetComponent<SphereCollider>().center = Vector3.zero;
        GetComponent<SphereCollider>().radius = 0.1f;
    }

    void updateTransform()
    {
        transform.localPosition = Utils.Coor2Vec(vertex.Coordinate);
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

    void updateRenderer()
    {
        float radius = Camera.main.transform.position.y * radiusFactor;
        float width = radius * widthFactor;

        LineRenderer lr = GetComponent<LineRenderer>();
        lr.positionCount = step;
        lr.SetPositions(CirclePosition(Utils.Coor2Vec(vertex.Coordinate), radius, step));
        lr.alignment = LineAlignment.TransformZ;
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;
        lr.sortingLayerID = sortingLayerId;
        lr.sortingOrder = sortingOrder;
        lr.material = material;
        if (highLight)
            lr.material.color = new Color(1.0f, 0.2f, 0.2f);
        else
            lr.material.color = new Color(0.2f, 1.0f, 0.2f);

        needUpdateRenderer = false;
    }

}
