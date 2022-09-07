using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class HierarchyPanelController : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    private UIEventSubscriber eventSubscriber;
    public UIDocument rootUIDocument;
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

    // TODO(robust): prevent switch simulation during simulation
    void Start()
    {
        foldoutContainer = rootUIDocument.rootVisualElement.Q<ScrollView>("FoldoutContainer");

        var tf = new TextField();

        gridMapFoldout = new Foldout();
        gridMapFoldout.text = "gridmap";
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

        OnAddSimulation += simName =>
            eventDispatcher.Raise(this, new UIEvent() { name = "add simulation", message = simName, type = UIEventType.Hierarchy });

        OnSelectSimulation += simName =>
            eventDispatcher.Raise(this, new UIEvent() { name = "select simulation", message = simName, type = UIEventType.Hierarchy });

        OnSelectIndoorMap += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "select indoor map", type = UIEventType.Hierarchy });

        OnSelectGridMap += gridName =>
            eventDispatcher.Raise(this, new UIEvent() { name = "select grid map", message = gridName, type = UIEventType.Hierarchy });

        eventSubscriber = new UIEventSubscriber(eventDispatcher);

        foldoutContainer.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(this, new UIEvent() { name = "hierarchy panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        foldoutContainer.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(this, new UIEvent() { name = "hierarchy panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
    }
    void Update()
    {
        eventSubscriber.ConsumeAll(EventListener);
    }

    public void UpdateGridMap(string gridmapIdList)
    {
        List<string> gridMapIds = new List<string>(gridmapIdList.Split('\n'));

        gridMapFoldout.Clear();
        CollapsesAll();
        gridMapFoldout.SetValueWithoutNotify(true);

        foreach (var id in gridMapIds)
            gridMapFoldout.Add(new TextElement() { text = id });
    }

    public void UpdateIndoorData(string json)
    {
        Debug.Log("Update indoor data");
        indoorMapFoldout.Clear();
        CollapsesAll();
        indoorMapFoldout.SetValueWithoutNotify(true);

        var jsonData = JObject.Parse(json);
        foreach (var vertexJson in jsonData["cellVertexMember"].Children())
            indoorMapFoldout.Add(new TextElement() { text = vertexJson["Id"].Value<string>() });
        foreach (var boundaryJson in jsonData["cellBoundaryMember"].Children())
            indoorMapFoldout.Add(new TextElement() { text = boundaryJson["Id"].Value<string>() });
        foreach (var spaceJson in jsonData["cellSpaceMember"].Children())
            indoorMapFoldout.Add(new TextElement() { text = spaceJson["Id"].Value<string>() });
    }

    public void UpdateSimulation(string json)
    {
        simFoldouts.ForEach(sim => foldoutContainer.Remove(sim));
        simFoldouts.Clear();
        CollapsesAll();
        foldoutContainer.Remove(createSim);
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
        Debug.Log("CollapsesAll");
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
