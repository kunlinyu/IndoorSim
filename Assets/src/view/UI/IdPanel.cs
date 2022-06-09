using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;

public class IdPanel : MonoBehaviour
{
    VisualElement idPanel;
    public void Init(VisualElement idPanel)
    {
        this.idPanel = idPanel;
        idPanel.visible = false;
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
