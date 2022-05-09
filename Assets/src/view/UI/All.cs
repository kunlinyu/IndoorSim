using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(ToolBarController))]
[RequireComponent(typeof(CursorTip))]
[RequireComponent(typeof(LogWindow))]
public class All : MonoBehaviour
{

    public UIEventDispatcher eventDispatcher;

    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        VisualElement toolBar = root.Q<VisualElement>("ToolBar");

        ToolBarController toolBarController = GetComponent<ToolBarController>();
        toolBarController.LoadButtons(toolBar, (button, tbd) =>
            { eventDispatcher.Raise(button, new UIEvent() { name = tbd.m_ToolName, type = UIEventType.ButtonClick }); });
        toolBar.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        toolBar.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });

        CursorTip cursorTip = GetComponent<CursorTip>();
        cursorTip.Init(root.Q<Label>("Tip"));
        eventDispatcher.eventListener += cursorTip.EventListener;

        Label versionLabel = root.Q<Label>("VersionLabel");
        versionLabel.text = " IndoorSim version: V" + Application.version;

        ListView listView = root.Q<ListView>("LogList");

        LogWindow logWindow = GetComponent<LogWindow>();
        logWindow.Init(root.Q<ListView>("LogList"));
    }

    void Update()
    {

    }
}
