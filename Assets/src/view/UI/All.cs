using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(ToolBarController))]
[RequireComponent(typeof(CursorTip))]
[RequireComponent(typeof(LogWindow))]
[RequireComponent(typeof(AssetsPanelController))]
[RequireComponent(typeof(HierarchyPanelController))]
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
        var assetsPanelController = GetComponent<AssetsPanelController>();
        var assetsPanel = root.Q<VisualElement>("AssetsPanel");
        assetsPanel.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "assets panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        assetsPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "assets panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
        assetsPanelController.Init(
            assetsPanel,
            (index) => { eventDispatcher.Raise(assetsPanelController, new UIEvent() { name = "apply asset", message = index.ToString(), type = UIEventType.ToolButtonClick }); },
            (index) => { eventDispatcher.Raise(assetsPanelController, new UIEvent() { name = "remove asset", message = index.ToString(), type = UIEventType.ToolButtonClick }); }
        );
        eventDispatcher.eventListener += assetsPanelController.EventListener;

        // hierarchy panel
        var hierarchyPanelController = GetComponent<HierarchyPanelController>();
        var hierarchyPanel = root.Q<ScrollView>("FoldoutContainer");

        hierarchyPanelController.Init(hierarchyPanel);

        hierarchyPanelController.OnAddSimulation += simName =>
            eventDispatcher.Raise(this, new UIEvent() { name = "add simulation", message = simName, type = UIEventType.Hierarchy });

        hierarchyPanelController.OnSelectSimulation += simName =>
            eventDispatcher.Raise(this, new UIEvent() { name = "select simulation", message = simName, type = UIEventType.Hierarchy });

        hierarchyPanelController.OnSelectIndoorMap += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "select indoor map", type = UIEventType.Hierarchy });

        hierarchyPanelController.OnSelectGridMap += gridName =>
            eventDispatcher.Raise(this, new UIEvent() { name = "select grid map", message = gridName , type = UIEventType.Hierarchy });

        eventDispatcher.eventListener += hierarchyPanelController.EventListener;
    }

    void Update()
    {

    }
}
