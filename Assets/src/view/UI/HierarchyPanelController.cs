using System.Collections;
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

    private bool placeHolderMode = true;
    private string placeHolderText = "new simulation name";

    void Start()
    {

    }

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
        indoorMapFoldout.Add(new TextElement() { text = "SPC0" });
        foldoutContainer.Add(indoorMapFoldout);

        simFoldouts = new List<Foldout>();
        Foldout sim1 = new Foldout();
        sim1.text = "fake simulation 1";
        sim1.Add(new TextElement() { text = "fake robot 1" });
        sim1.Add(new TextElement() { text = "fake robot 2" });
        sim1.Add(new TextElement() { text = "fake robot 3" });
        sim1.Add(new TextElement() { text = "fake robot 4" });
        sim1.Add(new TextElement() { text = "fake robot 5" });
        sim1.Add(new TextElement() { text = "fake robot 6" });
        simFoldouts.Add(sim1);
        foldoutContainer.Add(sim1);


        Foldout sim2 = new Foldout();
        sim2.text = "fake simulation 2";
        var ten = new TextElement() { text = "fake robot 1" };
        ten.RegisterCallback<ClickEvent>(evt =>
        {
            Debug.Log("sim2 clicked");
            TextElement textElement = evt.target as TextElement;
            textElement.style.backgroundColor = new StyleColor(new Color(1.0f, 1.0f, 1.0f));
        });
        sim2.Add(ten);
        simFoldouts.Add(sim2);
        foldoutContainer.Add(sim2);



        TextField newSim = new TextField();
        newSim.isReadOnly = false;
        newSim.value = "new simulation name";
        newSim.RegisterCallback<FocusInEvent>(evt =>
        {
            if (placeHolderMode)
            {
                var textField = evt.target as TextField;
                textField.value = "";
            }
        });
        newSim.RegisterCallback<FocusOutEvent>(evt =>
        {
            var textField = evt.target as TextField;
            Debug.Log("input field: " + textField.value);
            placeHolderMode = string.IsNullOrEmpty(textField.value);
            if (placeHolderMode)
                textField.value = placeHolderText;
        });

        foldoutContainer.Add(newSim);


    }

    public void UpdateContent(string json)
    {
        var jsonData = JArray.Parse(json);
        foreach (var assetJson in jsonData.Children())
        {
        }
    }

    public void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.Hierarchy && e.name == "gridmap")
            UpdateContent(e.message);
        else if (e.type == UIEventType.Hierarchy && e.name == "indoordata")
            UpdateContent(e.message);
        else if (e.type == UIEventType.Hierarchy && e.name == "simulation")
            UpdateContent(e.message);
    }

}
