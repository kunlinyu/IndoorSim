using System.Linq;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public class MousePickController : MonoBehaviour
{

    public const float radiusFactor = 0.02f;

    static private Selectable? selectedEntity = null;

    static public Selectable? SelectedEntity { get => selectedEntity; }

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

        Selectable? NearestEntity = null;
        float minDistance = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.GetComponent<VertexController>() != null)
            {
                VertexController vc = hit.collider.gameObject.GetComponent<VertexController>();
                float distance = vc.Distance(mousePositionOnGround);
                if (NearestEntity == null || minDistance > distance || NearestEntity.type != SelectableType.Vertex)
                {
                    NearestEntity = vc;
                    minDistance = distance;
                }
            }
            if (hit.collider.gameObject.GetComponent<BoundaryController>() != null)
            {
                if (NearestEntity != null && NearestEntity.type == SelectableType.Vertex) continue;
                BoundaryController vc = hit.collider.gameObject.GetComponent<BoundaryController>();
                float distance = vc.Distance(mousePositionOnGround);
                if (NearestEntity == null || minDistance > distance)
                {
                    NearestEntity = vc;
                    minDistance = distance;
                }
            }
        }

        if (NearestEntity != selectedEntity)
        {
            if (selectedEntity != null) selectedEntity.highLight = false;
            if (NearestEntity != null) NearestEntity.highLight = true;
            selectedEntity = NearestEntity;
        }
    }
}
