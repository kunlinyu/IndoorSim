using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GridMapImporter : MonoBehaviour
{
    public void Init(string filename, int width, int height, GridMapImageFormat format, Action<string> importAction)
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.Q<TextField>("file_name").value = filename;
        root.Q<TextField>("id").value = filename;
        root.Q<IntegerField>("width").value = width;
        root.Q<IntegerField>("height").value = height;
        root.Q<Button>("import").clicked += () => importAction(Serialize());

    }

    string Serialize()
    {
        return "serialize grid map panel";
    }

    void Update()
    {

    }
}
