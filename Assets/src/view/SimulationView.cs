using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SimulationView : MonoBehaviour
{
    public IndoorSimData indoorSimData;

    private GameObject agentParentObj;

    private Dictionary<AgentDescriptor, GameObject> agent2Obj = new Dictionary<AgentDescriptor, GameObject>();

    void Start()
    {
        agentParentObj = new GameObject("agent parent");
        agentParentObj.transform.SetParent(transform);
        agentParentObj.transform.localPosition = Vector3.zero;
        agentParentObj.transform.localRotation = Quaternion.identity;

        indoorSimData.OnAgentCreate += (agentDesc) =>
        {
            string prefabName = "unknown";
            if (agentDesc.type == "capsule")
                prefabName = "capsule";
            if (agentDesc.type == "boxcapsule")
                prefabName = "boxcapsule";
            if (agentDesc.type == "bronto")
                prefabName = "bronto";
            if (prefabName == "unknown")
                throw new System.Exception("unknown agent type: " + agentDesc.type);

            GameObject prefab = Resources.Load<GameObject>("Agent/" + prefabName);
            GameObject agentObj = Instantiate(prefab, agentParentObj.transform);
            IAgentHW agentHW = agentObj.GetComponent(typeof(IAgentHW)) as IAgentHW;
            agentHW.AgentDescriptor = agentDesc;
            agentHW.ResetToInitStatus();
            agent2Obj[agentDesc] = agentObj;
        };
        indoorSimData.OnAgentRemoved += (agent) =>
        {
            Destroy(agent2Obj[agent]);
            agent2Obj.Remove(agent);
        };
    }

    public List<IAgentHW> GetAgentHWs()
        => agent2Obj.Values.Select(obj => obj.GetComponent(typeof(IAgentHW)) as IAgentHW).ToList();

    void Update()
    {

    }
}
