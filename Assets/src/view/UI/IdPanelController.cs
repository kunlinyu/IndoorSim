using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;

public class IdPanelController : MonoBehaviour
{
    VisualElement idPanel;
    public void Init(VisualElement idPanel, Action<string, string> OnSave)
    {
        this.idPanel = idPanel;
        idPanel.visible = false;
        var spaceIdField = idPanel.Q<TextField>("spaceId");
        var childrenIdField = idPanel.Q<TextField>("childrenId");
        idPanel.Q<Button>("save").clicked += () => { OnSave.Invoke(spaceIdField.value, childrenIdField.value); };
    }

    public void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.PopUp && e.name == "id panel")
        {
            var jsonData = JObject.Parse(e.message);
            string predicate = jsonData["predicate"].Value<string>();
            if (predicate == "hide")
            {
                idPanel.visible = false;
            }
            else if (predicate == "popup")
            {
                int x = jsonData["x"].Value<int>();
                int y = jsonData["y"].Value<int>();
                idPanel.visible = true;
                idPanel.style.left = x;
                idPanel.style.bottom = y;
            }
        }
    }

}
