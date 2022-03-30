using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class env : MonoBehaviour
{
    private IndoorSim indoorSim = new IndoorSim();  // model
    public MapView mapView;  // view
    public SimulationController simController;  // controller
    void OnEnable()
    {
        mapView.indoorTiling = indoorSim.indoorTiling;
        simController.indoorSim = indoorSim;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
