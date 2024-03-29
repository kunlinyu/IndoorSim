using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SimulationView : MonoBehaviour
{
    public IndoorSimData indoorSimData;

    private GameObject agentParentObj;

    public Dictionary<AgentDescriptor, GameObject> agent2Obj = new Dictionary<AgentDescriptor, GameObject>();

    void Start()
    {
        agentParentObj = transform.Find("Agents").gameObject;

        indoorSimData.OnAgentCreate += (agentDesc) =>
        {
            string prefabName = agentDesc.type;
            GameObject prefab = Resources.Load<GameObject>("Agent/" + prefabName);
            GameObject agentObj = Instantiate(prefab, agentParentObj.transform);
            agentObj.transform.Find("AgentShadow").gameObject.SetActive(false);
            IActuatorSensor agentHW = agentObj.GetComponent(typeof(IActuatorSensor)) as IActuatorSensor;
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

    public List<IActuatorSensor> GetAgentHWs()
        => agent2Obj.Values.Select(obj => obj.GetComponent(typeof(IActuatorSensor)) as IActuatorSensor).ToList();

    void Update()
    {

    }
}
