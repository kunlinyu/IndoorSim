using UnityEngine;

public class env : MonoBehaviour
{
    private IndoorSimData indoorSimData = new IndoorSimData();  // model
    public IndoorMapView mapView;  // view
    public SimulationView simulationView;  // view
    public SimDataController simDataController;  // controller

    public SimulationController simController;  // controller

    void OnEnable()
    {
        mapView.indoorTiling = indoorSimData.indoorTiling;
        simulationView.indoorSimData = indoorSimData;

        simDataController.indoorSimData = indoorSimData;
        simDataController.mapView = mapView;
        simDataController.simView = simulationView;

        simController.indoorSimData = indoorSimData;
        simController.simulationView = simulationView;
    }

    void Update()
    {
    }
}
