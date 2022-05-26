using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class HierarchyPanelController : MonoBehaviour
{
    private ScrollView foldoutContainer;
    private Foldout gridMapFoldout;
    private Foldout indoorMapFoldout;
    private List<Foldout> simFoldouts;
    TextField createSim;

    private Action<string> OnAddSimulation;

    private string placeHolderText = "new simulation name";

    void Start()
    {

    }

    public void Init(ScrollView FoldoutContainer, Action<string> OnAddSimulation)
    {
        foldoutContainer = FoldoutContainer;
        this.OnAddSimulation = OnAddSimulation;


        var tf = new TextField();


        gridMapFoldout = new Foldout();
        gridMapFoldout.text = "gridmap";

        gridMapFoldout.Add(new TextElement() { text = "fake grid map 1.png" });
        gridMapFoldout.Add(new TextElement() { text = "fake grid map 2.png" });
        foldoutContainer.Add(gridMapFoldout);

        indoorMapFoldout = new Foldout();
        indoorMapFoldout.text = "map";
        indoorMapFoldout.Add(new TextElement() { text = "SPC0" });
        foldoutContainer.Add(indoorMapFoldout);

        simFoldouts = new List<Foldout>();

        createSim = new TextField();
        createSim.isReadOnly = false;
        createSim.value = "new simulation name";
        createSim.RegisterCallback<FocusInEvent>(evt =>
        {
            var textField = evt.target as TextField;
            textField.value = "";
        });
        createSim.RegisterCallback<FocusOutEvent>(evt =>
        {
            var textField = evt.target as TextField;
            if (textField.value != "")
                OnAddSimulation?.Invoke(textField.value);
            textField.value = placeHolderText;
        });

        foldoutContainer.Add(createSim);


    }

    public void UpdateContent(string json)
    {
        var jsonData = JArray.Parse(json);
        foreach (var assetJson in jsonData.Children())
        {
        }
    }

    public void UpdateIndoorData(string json)
    {
        indoorMapFoldout.Clear();

        var jsonData = JObject.Parse(json);
        foreach (var vertexJson in jsonData["vertexPool"].Children())
            indoorMapFoldout.Add(new TextElement() { text = vertexJson["Id"].Value<string>() });
        foreach (var boundaryJson in jsonData["boundaryPool"].Children())
            indoorMapFoldout.Add(new TextElement() { text = boundaryJson["Id"].Value<string>() });
        foreach (var spaceJson in jsonData["spacePool"].Children())
            indoorMapFoldout.Add(new TextElement() { text = spaceJson["Id"].Value<string>() });
    }

    public void UpdateSimulation(string json)
    {
        simFoldouts.ForEach(sim => foldoutContainer.Remove(sim));
        simFoldouts.Clear();
        foldoutContainer.Remove(createSim);

        var jsonData = JArray.Parse(json);
        Debug.Log(json);
        foreach (var simulationJson in jsonData.Children())
        {
            Foldout simFoldout = new Foldout();
            simFoldout.text = simulationJson["name"].Value<string>();
            simFoldouts.Add(simFoldout);
            foldoutContainer.Add(simFoldout);

            foreach (var agent in simulationJson["agents"])
                simFoldout.Add(new TextElement() { text = agent["name"].Value<string>() });
        }

        foldoutContainer.Add(createSim);
    }

    public void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.Hierarchy && e.name == "gridmap")
            UpdateContent(e.message);
        else if (e.type == UIEventType.Hierarchy && e.name == "indoordata")
            UpdateIndoorData(e.message);
        else if (e.type == UIEventType.Hierarchy && e.name == "simulation")
            UpdateSimulation(e.message);
    }

}
