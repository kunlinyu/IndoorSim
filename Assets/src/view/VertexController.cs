using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VertexController : MonoBehaviour
{
    private CellVertex vertex;
    public CellVertex Vertex
    {
        get { return vertex; }
        set
        {
            vertex = value;
            vertex.OnUpdate += updateRenderer;
        }
    }

    [SerializeField] public float widthFactor = 0.2f;
    [SerializeField] public float radiusFactor = 0.02f;
    [SerializeField] public int step = 16;
    [SerializeField] public int sortingLayerId = 0;
    [SerializeField] public int sortingOrder = 0;
    [SerializeField] public Material material;

    private int lastCameraHeightInt;


    // Start is called before the first frame update
    void Start()
    {
        GetComponent<LineRenderer>().positionCount = 0;
        material = new Material(Shader.Find("Sprites/Default"));
        material.color = new Color(1.0f, 0.8f, 0.8f);
    }

    // Update is called once per frame
    void Update()
    {
        int newHeightInt = (int) (Camera.main.transform.position.y * 0.5f);
        if (lastCameraHeightInt != newHeightInt)
        {
            lastCameraHeightInt = newHeightInt;
            updateRenderer();
        }
    }

    Vector3[] CirclePosition(Vector3 center, float radius, int step)
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
    }
}
