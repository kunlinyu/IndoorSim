using System.IO;
using System.IO.Compression;
using System.Text;

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

        indoorSimData.PostAction = () =>
        {
            byte[] key = Hash.GetHash(IndoorSimData.schemaHashHistory[Application.version]);
            var json = indoorSimData.Serialize(Application.version, false);
            Debug.Log("Length of json: " + json.Length);
            var zippedJson = Compress(Encoding.ASCII.GetBytes(json));
            Debug.Log("Length of zipped json: " + zippedJson.Length);
            // DebugInfoUploader.DebugInfo(zippedJson, key, false);
        };
    }

    // TODO repeat code
    public static byte[] Decompress(byte[] bytes)
    {
        using (var memoryStream = new MemoryStream(bytes))
        using (var outputStream = new MemoryStream())
        {
            using (var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                decompressStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }

    // TODO repeat code
    public static byte[] Compress(byte[] bytes)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, System.IO.Compression.CompressionLevel.Optimal))
                gzipStream.Write(bytes, 0, bytes.Length);
            return memoryStream.ToArray();
        }
    }

    void Update()
    {

    }
}
