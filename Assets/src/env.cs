using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class env : MonoBehaviour
{
    private IndoorSimData indoorSimData = new IndoorSimData();  // model
    public MapView mapView;  // view
    public SimulationView simulationView;  // view
    public SimDataController simController;  // controller

    void OnEnable()
    {
        mapView.indoorTiling = indoorSimData.indoorTiling;
        simulationView.indoorSimData = indoorSimData;

        simController.indoorSimData = indoorSimData;
        simController.mapView = mapView;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
