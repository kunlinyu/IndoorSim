using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class env : MonoBehaviour
{
    IndoorSim indoorSim = new IndoorSim();
    public Map map;
    public SimulationController simController;
    void OnEnable()
    {
        map.indoorTiling = indoorSim.indoorTiling;
        simController.indoorSim = indoorSim;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
