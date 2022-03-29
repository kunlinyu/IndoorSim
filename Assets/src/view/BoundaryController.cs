using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class BoundaryController : MonoBehaviour
{
    private CellBoundary boundary;
    public CellBoundary Boundary
    {
        get { return boundary; }
        set
        {
            boundary = value;
            boundary.OnUpdate += updateRenderer;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        updateRenderer();
        transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void updateRenderer()
    {

        LineRenderer lr = GetComponent<LineRenderer>();
        lr.positionCount = boundary.Geom.NumPoints;
        lr.SetPositions(boundary.Geom.Coordinates.Select(coor => Utils.Coor2Vec(coor)).ToArray());
        lr.alignment = LineAlignment.TransformZ;
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;
    }
}
