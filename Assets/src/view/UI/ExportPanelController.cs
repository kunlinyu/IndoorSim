using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;

[RequireComponent(typeof(UIDocument))]
public class ExportPanelController : MonoBehaviour
{
    VisualElement root;
    List<Exporter> allExporter = null;

    public void Init(List<string> layers, Action<string> exportAction, Action cancelAction)
    {
        allExporter = new List<Exporter>(Resources.LoadAll<Exporter>("Exporter"));

        root = GetComponent<UIDocument>().rootVisualElement;
        root.Q<DropdownField>("layer").choices = layers;
        root.Q<DropdownField>("layer").index = 0;

        root.Q<DropdownField>("file").choices = allExporter.Select(exporter => exporter.name).ToList();
        root.Q<DropdownField>("file").index = 0;
        SwitchInclude(root.Q<DropdownField>("file").value);
        root.Q<DropdownField>("file").RegisterValueChangedCallback((evt) => SwitchInclude(evt.newValue));

        root.Q<Button>("Cancel").clicked += () => cancelAction?.Invoke();

        root.Q<Button>("Export").clicked += () =>
        {
            string exportInfo = $"{{\"layer\":\"{root.Q<DropdownField>("layer").text}\"," +
                                $"\"file\":\"{root.Q<DropdownField>("file").text}\"," +
                                $"\"include\":\"{root.Q<Toggle>("include").value}\"}}";
            exportAction?.Invoke(exportInfo);
        };
    }

    private void SwitchInclude(string exporterName)
    {
        Exporter exporter = allExporter.Find(exporter => exporter.name == exporterName);
        root.Q<Toggle>("include").SetEnabled(exporter.canIncludeFull);
        if (exporter.canIncludeFull == false)
            root.Q<Toggle>("include").value = false;
    }
}
