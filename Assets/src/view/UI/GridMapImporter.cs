using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;

struct GridMapInfo {
    public string id;
    public double resolution;
    public double origin_x;
    public double origin_y;
    public double origin_theta;
    public string zipBase64Image;
}

[RequireComponent(typeof(UIDocument))]
public class GridMapImporter : MonoBehaviour
{
    public void Init(string filename, int width, int height, GridMapImageFormat format, string zippedBase64Image, Action<string> importAction)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.Q<TextField>("file_name").value = filename;
        root.Q<TextField>("id").value = Path.GetFileName(filename);
        root.Q<IntegerField>("width").value = width;
        root.Q<IntegerField>("height").value = height;
        root.Q<Button>("import").clicked += () => importAction(Serialize(zippedBase64Image));

    }

    string Serialize(string zipBase64Image)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        GridMapInfo gridMapInfo = new GridMapInfo();

        gridMapInfo.id = root.Q<TextField>("id").value;
        gridMapInfo.resolution = root.Q<DoubleField>("resolution").value;
        gridMapInfo.origin_x = root.Q<Vector2Field>("origin").value.x;
        gridMapInfo.origin_y = root.Q<Vector2Field>("origin").value.y;
        gridMapInfo.origin_theta = root.Q<DoubleField>("origin_theta").value;
        gridMapInfo.zipBase64Image = zipBase64Image;
        return JsonConvert.SerializeObject(gridMapInfo);
    }

    void Update()
    {

    }
}
