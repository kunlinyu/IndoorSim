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

    public Action<string> OnAddSimulation;
    public Action<string> OnSelectSimulation;
    public Action OnSelectIndoorMap;
    public Action<string> OnSelectGridMap;

    private string placeHolderText = "new simulation name";

    private string simFoldoutPrefix = "simulation ";

    void Start()
    {

    }

    // TODO(robust): prevent switch simulation during simulation

    public void Init(ScrollView FoldoutContainer)
    {
        foldoutContainer = FoldoutContainer;

        var tf = new TextField();


        gridMapFoldout = new Foldout();
        gridMapFoldout.text = "gridmap";

        gridMapFoldout.Add(new TextElement() { text = "fake grid map 1.png" });
        gridMapFoldout.Add(new TextElement() { text = "fake grid map 2.png" });
        foldoutContainer.Add(gridMapFoldout);

        indoorMapFoldout = new Foldout();
        indoorMapFoldout.text = "map";
        indoorMapFoldout.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("indoorMap clicked");
            CollapsesAll();
            indoorMapFoldout.SetValueWithoutNotify(true);
            OnSelectIndoorMap?.Invoke();
        });
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

    public void UpdateGridMap(string json)
    {
        var jsonData = JArray.Parse(json);
        foreach (var assetJson in jsonData.Children())
        {
        }
    }

    public void UpdateIndoorData(string json)
    {
        indoorMapFoldout.Clear();
        CollapsesAll();
        indoorMapFoldout.SetValueWithoutNotify(true);

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
        CollapsesAll();
        foldoutContainer.Remove(createSim);
        Debug.Log(json);
        var jsonData = JArray.Parse(json);
        foreach (var simulationJson in jsonData.Children())
        {
            Foldout simFoldout = new Foldout();
            simFoldout.text = simFoldoutPrefix + simulationJson["name"].Value<string>();

            bool active = simulationJson["active"].Value<bool>();
            simFoldout.SetValueWithoutNotify(active);

            simFoldouts.Add(simFoldout);
            foldoutContainer.Add(simFoldout);
            simFoldout.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log("simFoldout clicked");
                OnSelectSimulation?.Invoke(simFoldout.text.Substring(simFoldoutPrefix.Length));
            });
            foreach (var agent in simulationJson["agents"])
                simFoldout.Add(new TextElement() { text = agent["name"].Value<string>() });
        }

        foldoutContainer.Add(createSim);
    }

    private void CollapsesAll()
    {
        gridMapFoldout.SetValueWithoutNotify(false);
        indoorMapFoldout.SetValueWithoutNotify(false);
        simFoldouts.ForEach(foldout => foldout.SetValueWithoutNotify(false));
    }

    public void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.Hierarchy && e.name == "gridmap")
            UpdateGridMap(e.message);
        else if (e.type == UIEventType.Hierarchy && e.name == "indoordata")
            UpdateIndoorData(e.message);
        else if (e.type == UIEventType.Hierarchy && e.name == "simulation")
            UpdateSimulation(e.message);
    }

}
