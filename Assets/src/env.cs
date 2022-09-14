using UnityEngine;

public class env : MonoBehaviour
{
    private IndoorSimData indoorSimData = new IndoorSimData(true);  // model

    public GridMapView gridMapView;  // view
    public IndoorMapView indoorMapView;  // view
    public SimulationView simulationView;  // view

    public SimDataController simDataController;  // controller
    public SimulationController simController;  // controller

    void OnEnable()
    {
        UnitySystemConsoleRedirector.Redirect();

        gridMapView.indoorSimData = indoorSimData;
        indoorMapView.indoorFeatures = indoorSimData.indoorFeatures;
        simulationView.indoorSimData = indoorSimData;

        simDataController.indoorSimData = indoorSimData;
        simDataController.mapView = indoorMapView;
        simDataController.simView = simulationView;

        simController.indoorSimData = indoorSimData;
        simController.simulationView = simulationView;
    }

    void Update()
    {
    }
}
