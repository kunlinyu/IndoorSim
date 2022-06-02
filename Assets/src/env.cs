using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class env : MonoBehaviour
{
    private IndoorSimData indoorSimData = new IndoorSimData();  // model
    public MapView mapView;  // view
    public SimulationView simulationView;  // view
    public SimDataController simDataController;  // controller

    public SimulationController simController;  // controller

    void OnEnable()
    {
        mapView.indoorTiling = indoorSimData.indoorTiling;
        simulationView.indoorSimData = indoorSimData;

        simDataController.indoorSimData = indoorSimData;
        simDataController.mapView = mapView;

        simController.indoorSimData = indoorSimData;
        simController.simulationView = simulationView;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
