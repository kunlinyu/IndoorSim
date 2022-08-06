using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;

[RequireComponent(typeof(UIDocument))]
public class IdPanelController : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    VisualElement idPanel;
    TextField containerIdField;
    TextField childrenIdField;

    void Update()
    {
        // This may be a bug of UI Toolkit. We implement backspace logic manually
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

    public void Init(string containerId, string childrenId, int x, int y, UIEventDispatcher eventDispatcher)
    {
        idPanel.style.left = x;
        idPanel.style.bottom = y;
        containerIdField.value = containerId;
        childrenIdField.value = childrenId;
        this.eventDispatcher = eventDispatcher;
    }

    void Start()
    {
        Debug.Log("Start of IdPanelController");
    }

    void OnEnable()
    {
        Debug.Log("OnEnable of IdPanelController");
        idPanel = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("IdPanel");
        Debug.Log(GetComponent<UIDocument>().rootVisualElement.name);

        containerIdField = idPanel.Q<TextField>("containerId");
        childrenIdField = idPanel.Q<TextField>("childrenId");

        idPanel.Q<Button>("cancel").clicked += () =>
        {
            eventDispatcher.Raise(this, new UIEvent()
            {
                name = "id panel",
                message = $"{{\"predicate\":\"hide\"}}",
                type = UIEventType.PopUp
            });
        };
        idPanel.Q<Button>("save").clicked += () =>
        {
            eventDispatcher.Raise(this, new UIEvent()
            {
                name = "container id",
                message = $"{{\"containerId\":\"{containerIdField.value}\",\"childrenId\":\"{childrenIdField.value}\"}}",
                type = UIEventType.IndoorSimData
            });
            eventDispatcher.Raise(this, new UIEvent()
            {
                name = "id panel",
                message = $"{{\"predicate\":\"hide\"}}",
                type = UIEventType.PopUp
            });
        };

        idPanel.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(idPanel, new UIEvent() { name = "id panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        idPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(idPanel, new UIEvent() { name = "id panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
    }

    void OnDestroy()
    {
        eventDispatcher.Raise(idPanel, new UIEvent() { name = "id panel", message = "leave", type = UIEventType.EnterLeaveUIPanel });
    }

}
