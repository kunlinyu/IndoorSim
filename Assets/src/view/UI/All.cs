using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(ToolBarController))]
[RequireComponent(typeof(CursorTip))]
[RequireComponent(typeof(LogWindow))]
[RequireComponent(typeof(AssetsPanelController))]
public class All : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;

    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        // toolBar
        VisualElement toolBar = root.Q<VisualElement>("ToolBar");
        ToolBarController toolBarController = GetComponent<ToolBarController>();
        toolBarController.LoadButtons(toolBar, (button, tbd) =>
            { eventDispatcher.Raise(button, new UIEvent() { name = tbd.m_ToolName, type = UIEventType.ToolButtonClick }); });
        toolBar.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        toolBar.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });

        // cursor tip
        CursorTip cursorTip = GetComponent<CursorTip>();
        cursorTip.Init(root.Q<Label>("Tip"));
        eventDispatcher.eventListener += cursorTip.EventListener;

        // version label
        Label versionLabel = root.Q<Label>("VersionLabel");
        versionLabel.text = " IndoorSim version: V" + Application.version;

        // log window
        GetComponent<LogWindow>().Init(root.Q<ListView>("LogList"));

        // assets panel
        var assetsPanel = GetComponent<AssetsPanelController>();
        assetsPanel.Init(
            root.Q<VisualElement>("AssetsPanel"),
            (index) => { eventDispatcher.Raise(assetsPanel, new UIEvent() { name = "apply asset", message = index.ToString(), type = UIEventType.ToolButtonClick }); },
            (index) => { eventDispatcher.Raise(assetsPanel, new UIEvent() { name = "remove asset", message = index.ToString(), type = UIEventType.ToolButtonClick }); }
        );
        eventDispatcher.eventListener += assetsPanel.EventListener;

    }

    void Update()
    {

    }
}
