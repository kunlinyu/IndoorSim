using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class env : MonoBehaviour
{
    private IndoorSim indoorSim = new IndoorSim(new SimpleIDGenerator("VTX"), new SimpleIDGenerator("BDR"), new SimpleIDGenerator("SPC"));  // model
    public MapView mapView;  // view

    public SimulationController simController;  // controller
    public ToolBarController toolBarController;

    public UIEventDispatcher eventDispatcher;

    void OnEnable()
    {
        mapView.indoorTiling = indoorSim.indoorTiling;
        simController.indoorSim = indoorSim;
        simController.mapView = mapView;
        // toolBarController.eventDispatcher = eventDispatcher;
        // simController.eventDispatcher = eventDispatcher;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
