using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
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
            Debug.Log("Create view of boundary");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        updateRenderer();
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
        // lr.sortingLayerID = sortingLayerId;
        // lr.sortingOrder = sortingOrder;
        // lr.material = material;
    }
}
