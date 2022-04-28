using System.Linq;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public enum CurrentPickType
{
    All,
    Vertex,
    Boundary,
    Space,
    RLine,
}

public class MousePickController : MonoBehaviour
{

    public const float radiusFactor = 0.02f;

    static private Selectable? pointedEntity = null;
    static public Selectable? PointedEntity { get => pointedEntity; }

    static private VertexController? pointedVertex = null;
    static public VertexController? PointedVertex { get => pointedVertex; }

    static private BoundaryController? pointedBoundary = null;
    static public BoundaryController? PointedBoundary { get => pointedBoundary; }

    static private SpaceController? pointedSpace = null;
    static public SpaceController? PointedSpace { get => pointedSpace; }

    static private RLineController? pointedRLine = null;
    static public RLineController? PointedRLine { get => pointedRLine; }

    public UIEventDispatcher? eventDispatcher;

    static public CurrentPickType pickType { get; set; } = CurrentPickType.All;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        Vector3? mousePositionOnGroundNullable = CameraController.mousePositionOnGround();
        if (mousePositionOnGroundNullable == null) return;
        Vector3 mousePositionOnGround = mousePositionOnGroundNullable ?? throw new System.Exception("Oops");

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float radius = Camera.main.transform.position.y * radiusFactor;
        RaycastHit[] hits = Physics.SphereCastAll(Camera.main.transform.position, radius, ray.direction, 100.0f);

        VertexController? nearestVertex = null;
        BoundaryController? nearestBoundary = null;
        SpaceController? nearestSpace = null;
        RLineController? nearestRLine = null;
        float vertexMinDistance = float.MaxValue;
        float boundaryMinDistance = float.MaxValue;
        float spaceMinDistance = float.MaxValue;
        float rLineMinDistance = float.MaxValue;


        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.GetComponent<VertexController>() != null)
            {
                VertexController vc = hit.collider.gameObject.GetComponent<VertexController>();
                float distance = vc.Distance(mousePositionOnGround);
                if (vertexMinDistance > distance)
                {
                    nearestVertex = vc;
                    vertexMinDistance = distance;
                }
            }
            if (hit.collider.gameObject.GetComponent<BoundaryController>() != null)
            {
                BoundaryController bc = hit.collider.gameObject.GetComponent<BoundaryController>();
                float distance = bc.Distance(mousePositionOnGround);
                if (boundaryMinDistance > distance)
                {
                    nearestBoundary = bc;
                    boundaryMinDistance = distance;
                }
            }
            if (hit.collider.gameObject.GetComponent<SpaceController>() != null)
            {
                SpaceController sc = hit.collider.gameObject.GetComponent<SpaceController>();
                float distance = sc.Distance(mousePositionOnGround);
                if (spaceMinDistance > distance)
                {
                    nearestSpace = sc;
                    spaceMinDistance = distance;
                }
            }
            if (hit.collider.gameObject.GetComponent<RLineController>() != null)
            {
                RLineController rc = hit.collider.gameObject.GetComponent<RLineController>();
                float distance = rc.Distance(mousePositionOnGround);
                if (rLineMinDistance > distance)
                {
                    nearestRLine = rc;
                    rLineMinDistance = distance;
                }
            }
        }

        pointedVertex = nearestVertex;
        pointedBoundary = nearestBoundary;
        pointedSpace = nearestSpace;
        pointedRLine = nearestRLine;

        Selectable? nearestEntity = null;


        if (pickType == CurrentPickType.All)
        {
            if (nearestVertex != null)
                nearestEntity = nearestVertex;
            else if (nearestBoundary != null)
                nearestEntity = nearestBoundary;
            else if (nearestRLine != null)
                nearestEntity = nearestRLine;
            else if (nearestSpace != null)
                nearestEntity = nearestSpace;
        }
        else if (pickType == CurrentPickType.Vertex)
            nearestEntity = nearestVertex;
        else if (pickType == CurrentPickType.Boundary)
            nearestEntity = nearestBoundary;
        else if (pickType == CurrentPickType.Space)
            nearestEntity = nearestSpace;
        else if (pickType == CurrentPickType.RLine)
            nearestEntity = nearestRLine;
        else throw new System.Exception("unknown pick type: " + pickType);

        if (nearestEntity != pointedEntity)
        {
            if (pointedEntity != null) pointedEntity.highLight = false;
            if (nearestEntity != null) nearestEntity.highLight = true;
            pointedEntity = nearestEntity;

            eventDispatcher!.Raise(this, new UIEvent() { type = UIEventType.SceneTip, message = pointedEntity == null ? "" : pointedEntity.Tip() });
        }
    }
}
