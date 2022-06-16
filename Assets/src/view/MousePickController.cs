using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

[Flags]
public enum CurrentPickType : short
{
    None = 0,
    Vertex = 1,
    Boundary = 2,
    Space = 4,
    RLine = 8,
    Agent = 16,
    All = 31,
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

    static private AgentController? pointedAgent = null;
    static public AgentController? PointedAgent { get => pointedAgent; }

    public UIEventDispatcher? eventDispatcher;

    static public CurrentPickType pickType { get; set; } = CurrentPickType.All;

    void Update()
    {
        Vector3? mousePositionOnGroundNullable = CameraController.mousePositionOnGround();
        if (mousePositionOnGroundNullable == null) return;
        Vector3 mousePositionOnGround = mousePositionOnGroundNullable ?? throw new System.Exception("Oops");

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float radius = Camera.main.transform.position.y * radiusFactor;
        RaycastHit[] hits = Physics.SphereCastAll(Camera.main.transform.position, radius, ray.direction, 100.0f, LayerMask.GetMask("Default"));

        VertexController? nearestVertex = null;
        BoundaryController? nearestBoundary = null;
        SpaceController? nearestSpace = null;
        RLineController? nearestRLine = null;
        AgentController? nearestAgent = null;
        float vertexMinDistance = float.MaxValue;
        float boundaryMinDistance = float.MaxValue;
        float spaceMinDistance = float.MaxValue;
        float rLineMinDistance = float.MaxValue;
        float agentMinDistance = float.MaxValue;


        foreach (RaycastHit hit in hits)
        {
            var vc = hit.collider.gameObject.GetComponent<VertexController>();
            if (vc != null)
            {
                float distance = vc.Distance(mousePositionOnGround);
                if (vertexMinDistance > distance)
                {
                    nearestVertex = vc;
                    vertexMinDistance = distance;
                }
            }
            var bc = hit.collider.gameObject.GetComponent<BoundaryController>();
            if (bc != null)
            {
                float distance = bc.Distance(mousePositionOnGround);
                if (boundaryMinDistance > distance)
                {
                    nearestBoundary = bc;
                    boundaryMinDistance = distance;
                }
            }
            var sc = hit.collider.gameObject.GetComponent<SpaceController>();
            if (sc != null)
            {
                float distance = sc.Distance(mousePositionOnGround);
                if (spaceMinDistance > distance)
                {
                    nearestSpace = sc;
                    spaceMinDistance = distance;
                }
            }
            var rc = hit.collider.gameObject.GetComponent<RLineController>();
            if (rc != null)
            {
                float distance = rc.Distance(mousePositionOnGround);
                if (rLineMinDistance > distance)
                {
                    nearestRLine = rc;
                    rLineMinDistance = distance;
                }
            }

            var ac = hit.collider.gameObject.GetComponentInParent(typeof(AgentController)) as AgentController;
            if (ac != null)
            {
                float distance = ac.Distance(mousePositionOnGround);
                if (rLineMinDistance > distance)
                {
                    nearestAgent = ac;
                    agentMinDistance = distance;
                }
            }
        }

        pointedVertex = nearestVertex;
        pointedBoundary = nearestBoundary;
        pointedSpace = nearestSpace;
        pointedRLine = nearestRLine;
        pointedAgent = nearestAgent;

        Selectable? nearestEntity = null;

        if ((pickType & CurrentPickType.Space) == CurrentPickType.Space)
            if (nearestSpace != null)
                nearestEntity = nearestSpace;

        if ((pickType & CurrentPickType.RLine) == CurrentPickType.RLine)
            if (nearestRLine != null)
                nearestEntity = nearestRLine;

        if ((pickType & CurrentPickType.Boundary) == CurrentPickType.Boundary)
            if (nearestBoundary != null)
                nearestEntity = nearestBoundary;

        if ((pickType & CurrentPickType.Vertex) == CurrentPickType.Vertex)
            if (nearestVertex != null)
                nearestEntity = nearestVertex;

        if ((pickType & CurrentPickType.Agent) == CurrentPickType.Agent)
            if (nearestAgent != null)
                nearestEntity = nearestAgent;

        if (nearestEntity != pointedEntity)
        {
            if (pointedEntity != null) pointedEntity.highLight = false;
            if (nearestEntity != null) nearestEntity.highLight = true;
            pointedEntity = nearestEntity;

            eventDispatcher!.Raise(this, new UIEvent() { type = UIEventType.SceneTip, message = pointedEntity == null ? "" : pointedEntity.Tip() });
        }
    }
}
