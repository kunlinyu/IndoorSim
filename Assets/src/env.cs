using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class env : MonoBehaviour
{
    private IndoorSimData indoorSim = new IndoorSimData();  // model
    public MapView mapView;  // view
    public SimulationController simController;  // controller

    void OnEnable()
    {
        mapView.indoorTiling = indoorSim.indoorTiling;
        simController.indoorSimData = indoorSim;
        simController.mapView = mapView;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
