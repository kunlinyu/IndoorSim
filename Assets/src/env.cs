using System;
using System.Globalization;
using UnityEngine;


public class env : MonoBehaviour
{
    private IndoorSimData indoorSimData = new IndoorSimData(true);  // model

    public GridMapView gridMapView;  // view
    public IndoorMapView indoorMapView;  // view
    public SimulationView simulationView;  // view

    public SimDataController simDataController;  // controller
    public SimulationController simController;  // controller

    public DebugInfoUploader uploader;
    int count = 0;
    static readonly int kTriggerThresHold = 5;

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

        uploader.Key = Hash.GetHash(IndoorSimData.schemaHashHistory[Application.version]);
        Debug.Log(BitConverter.ToString(uploader.Key));


        indoorSimData.PostAction = () =>
        {
            count++;
            if (count >= kTriggerThresHold) count = 0;
            if (count != 0) return;
            string mapId = indoorSimData.Uuid.ToString();
            string latestUpdateTime = indoorSimData.latestUpdateTime?.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffffK");
            uploader.Append(() => indoorSimData.Serialize(Application.version, false), mapId, latestUpdateTime);
        };
        indoorSimData.PostActionAfterException = () =>
        {
            string mapId = indoorSimData.Uuid.ToString();
            string latestUpdateTime = indoorSimData.latestUpdateTime?.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'ffffffK");
            uploader.Append(() => indoorSimData.Serialize(Application.version, false), mapId, latestUpdateTime);
        };
    }
    void Update()
    {

    }
}
