using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable
public class AgentEditor : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public MapView? mapView { get; set; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
    public bool MouseOnUI { set; get; }

    private const double collisionRadius = 1.0f;

    public string agentType = "";

#pragma warning disable CS8618
    public GameObject shadow;
#pragma warning restore CS8618

    void Start()
    {
        var prefab = Resources.Load<GameObject>("AgentShadow");
        shadow = Instantiate(prefab, Vector3.zero, Quaternion.Euler(90.0f, 0.0f, 0.0f));
        shadow.transform.SetParent(transform);
        shadow.GetComponent<AgentShadowController>().freeMat = Resources.Load<Material>("Materials/agent shadow free");
        shadow.GetComponent<AgentShadowController>().collidedMat = Resources.Load<Material>("Materials/agent shadow collided");
        shadow.GetComponent<AgentShadowController>().radius = Resources.Load<AgentTypeMeta>("AgentTypeMeta/" + agentType).collisionRadius;
    }

    void Update()
    {
        Vector3? mousePosition = CameraController.mousePositionOnGround();
        if (mousePosition != null)
        {
            shadow.SetActive(true);
            shadow.transform.position = mousePosition.Value;
            shadow.GetComponent<AgentShadowController>().center = mousePosition.Value;
        }
        else
        {
            shadow.SetActive(false);
        }

        if (Input.GetMouseButtonUp(0) && mousePosition != null && !MouseOnUI)
        {
            AgentDescriptor agent = new AgentDescriptor();
            agent.name = agentType + " agent";
            agent.type = agentType;
            agent.x = mousePosition.Value.x;
            agent.y = mousePosition.Value.z;
            agent.theta = 0.0f;
            agent.containerId = null;
            IndoorSimData?.AddAgent(agent);
        }
    }
}
