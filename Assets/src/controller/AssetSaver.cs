using System;
using System.Linq;
using UnityEngine;

#nullable enable

public class AssetSaver : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public IndoorMapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
    public bool MouseOnUI { set; get; }

    public void ExtractSelected2Asset()
    {
        if (mapView == null) throw new System.Exception("mapView null");
        var selectedVertices = mapView.vertex2Obj.Select((entry, index) => entry.Value.GetComponent<VertexController>()).Where(vc => vc.selected).ToList();
        var selectedBoundaries = mapView.boundary2Obj.Select((entry, index) => entry.Value.GetComponent<BoundaryController>()).Where(bc => bc.selected).ToList();
        var selectedSpaces = mapView.cellspace2Obj.Select((entry, index) => entry.Value.GetComponent<SpaceController>()).Where(sc => sc.selected).ToList();
        // TODO(debt): selected agents

        if (selectedVertices.Count > 0 && selectedBoundaries.Count > 0)
            IndoorSimData?.ExtractAsset("untitled asdf",
                selectedVertices.Select(vc => vc.Vertex).ToList(),
                selectedBoundaries.Select(bc => bc.Boundary).ToList(),
                selectedSpaces.Select(sc => sc.Space).ToList(),
                capture);
        else
            Debug.LogWarning("nothing can be save as asset");
    }

    private string capture(float maxX, float minX, float maxY, float minY)
    {
        return capture(mapView, GetComponent<Camera>(), maxX, minX, maxY, minY);
    }

    public static string capture(IndoorMapView? mapView, Camera? screenshotCamera, float maxX, float minX, float maxY, float minY)
    {
        if (mapView == null) return "";
        if (screenshotCamera == null) return "";

        foreach (var entry in mapView.vertex2Obj)
            if (!entry.Value.GetComponent<VertexController>().selected)
                entry.Value.SetActive(false);
        foreach (var entry in mapView.boundary2Obj)
            if (!entry.Value.GetComponent<BoundaryController>().selected)
                entry.Value.SetActive(false);
        foreach (var entry in mapView.cellspace2Obj)
            if (!entry.Value.GetComponent<SpaceController>().selected)
            {
                entry.Value.SetActive(false);
                mapView.cellspace2RLineObj[entry.Key.rLines].SetActive(false);
            }

        int resWidth = 128;
        int resHeight = 128;
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);

        screenshotCamera.orthographicSize = Mathf.Max(maxX - minX, maxY - minY);
        Vector3 position = screenshotCamera.transform.position;
        position.x = (maxX + minX) / 2.0f;
        position.z = (maxY + minY) / 2.0f;
        screenshotCamera.transform.position = position;

        screenshotCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        screenshotCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        screenshotCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        byte[] bytes = screenShot.EncodeToPNG();
        string filename = ScreenShotName(resWidth, resHeight);
        System.IO.File.WriteAllBytes(filename, bytes);
        Debug.Log(string.Format("Took screenshot to: {0}", filename));


        foreach (var entry in mapView.vertex2Obj)
            entry.Value.SetActive(true);
        foreach (var entry in mapView.boundary2Obj)
            entry.Value.SetActive(true);
        foreach (var entry in mapView.cellspace2Obj)
            entry.Value.SetActive(true);
        foreach (var entry in mapView.cellspace2RLineObj)
            entry.Value.SetActive(true);
        return Convert.ToBase64String(bytes);
    }

    private static string ScreenShotName(int width, int height)
    {
        return string.Format("{0}/screen_{1}x{2}_{3}.png",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }
}
