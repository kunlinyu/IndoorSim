using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;

public class IdPanelController : MonoBehaviour
{
    VisualElement idPanel;

    void Update()
    {
        // This may be a bug of UI Toolkit. We implement backspace logic manually
        var containerIdField = this.idPanel.Q<TextField>("containerId");
        var childrenIdField = this.idPanel.Q<TextField>("childrenId");
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (containerIdField.panel.focusController.focusedElement == containerIdField)
                BackSpace(containerIdField);
            if (childrenIdField.panel.focusController.focusedElement == childrenIdField)
                BackSpace(childrenIdField);
        }
    }

    // This may be a bug of UI Toolkit. We implement backspace logic manually
    private static void BackSpace(TextField textField)
    {
        int cursorIndex = textField.cursorIndex;
        string value = textField.value;

        string leftPart = value.Substring(0, cursorIndex - 1 >= 0 ? cursorIndex - 1 : 0);
        string rightPart = value.Substring(cursorIndex);
        textField.value = leftPart + rightPart;

        if (cursorIndex > 0)
        {
            textField.cursorIndex = cursorIndex - 1;
            textField.selectIndex = cursorIndex - 1;
        }
    }

    public void Init(VisualElement idPanel, Action<string, string> OnSave)
    {
        this.idPanel = idPanel;
        // this.idPanel.visible = false;
        var containerIdField = this.idPanel.Q<TextField>("containerId");
        var childrenIdField = this.idPanel.Q<TextField>("childrenId");
        this.idPanel.Q<Button>("cancel").clicked += () =>
        {
            containerIdField.value = "";
            childrenIdField.value = "";
            this.idPanel.visible = false;
        };
        this.idPanel.Q<Button>("save").clicked += () =>
        {
            OnSave.Invoke(containerIdField.value, childrenIdField.value);
            containerIdField.value = "";
            childrenIdField.value = "";
            this.idPanel.visible = false;
        };
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
                string containerId = jsonData["containerId"].Value<string>();
                string childrenId = jsonData["childrenId"].Value<string>();
                idPanel.visible = true;
                idPanel.style.left = x;
                idPanel.style.bottom = y;
                idPanel.Q<TextField>("containerId").value = containerId;
                idPanel.Q<TextField>("childrenId").value = childrenId;
            }
        }
    }

}
