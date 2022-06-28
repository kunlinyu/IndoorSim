using System.Linq;
using UnityEngine;
#nullable enable
public class AgentEditor : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public MapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
    public bool MouseOnUI { set; get; }

    public string agentType = "";

#pragma warning disable CS8618
    public GameObject agent;
#pragma warning restore CS8618

    void Start()
    {
        GameObject agentPrefab = Resources.Load<GameObject>("Agent/" + agentType);
        agent = Instantiate(agentPrefab, transform);
        SetLayerRecursively(agent, LayerMask.NameToLayer("ToolDraft"));
    }

    public static void SetLayerRecursively(GameObject obj, int layerNumber)
    {
        foreach (Transform trans in obj.GetComponentsInChildren<Transform>(true))
            trans.gameObject.layer = layerNumber;
    }


    void Update()
    {
        bool anyCollided = false;
        Vector3? mousePosition = CameraController.mousePositionOnGround();
        if (mousePosition != null)
        {
            agent.SetActive(true);
            agent.transform.position = mousePosition.Value;

            float newRadius = agent.GetComponent<AgentController>().meta!.collisionRadius;

            if (IndoorSimData!.currentSimData == null)
                Debug.LogError("currentSimData null");

            anyCollided = IndoorSimData!.currentSimData!.agents.Any(agent =>
            {
                Vector3 agentDescPosition = new Vector3(agent.x, 0.0f, agent.y);
                float magnitude = (agentDescPosition - mousePosition.Value).magnitude;
                float radius = IndoorSimData!.agentMetaList[agent.type].collisionRadius;
                return magnitude < newRadius + radius;
            });

            if (anyCollided)
                agent.GetComponentInChildren<AgentShadowController>().collided = true;
            else
                agent.GetComponentInChildren<AgentShadowController>().collided = false;
        }
        else
        {
            agent.SetActive(false);
        }

        if (Input.GetMouseButtonUp(0) && mousePosition != null && !anyCollided && !MouseOnUI)
        {
            AgentDescriptor agentDesc = new AgentDescriptor();
            agentDesc.name = agentType + " agent";
            agentDesc.type = agentType;
            agentDesc.x = mousePosition.Value.x;
            agentDesc.y = mousePosition.Value.z;
            agentDesc.theta = 0.0f;
            agentDesc.containerId = null;
            IndoorSimData?.AddAgent(agentDesc, agent.GetComponent<AgentController>().meta!.ToNoneUnity());
        }
    }
}
