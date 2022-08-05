using System;
using System.IO;
using System.Text;
using System.Collections;
using System.IO.Compression;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;

using SFB;

[RequireComponent(typeof(ToolBarController))]
[RequireComponent(typeof(CursorTip))]
[RequireComponent(typeof(AssetsPanelController))]
[RequireComponent(typeof(HierarchyPanelController))]
[RequireComponent(typeof(IdPanelController))]
public class All : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    VisualElement root;

    void Start()
    {
        // TODO: move everthing here to separate object
        root = GetComponent<UIDocument>().rootVisualElement;

        // toolBar
        VisualElement toolBar = root.Q<VisualElement>("ToolBar");
        var toolBarController = GetComponent<ToolBarController>();
        toolBarController.LoadButtons(toolBar,
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "trigger", type = UIEventType.ToolButton }); },
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "enter", type = UIEventType.ToolButton }); },
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = "tool bar", message = "cancel", type = UIEventType.ToolButton }); });
        toolBar.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        toolBar.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });

        // cursor tip
        CursorTip cursorTip = GetComponent<CursorTip>();
        cursorTip.Init(root.Q<Label>("Tip"));
        eventDispatcher.eventListener += cursorTip.EventListener;

        // assets panel
        var assetsPanelController = GetComponent<AssetsPanelController>();
        var assetsPanel = root.Q<VisualElement>("AssetsPanel");
        assetsPanel.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "assets panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        assetsPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "assets panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
        assetsPanelController.Init(
            assetsPanel,
            (index) => { eventDispatcher.Raise(assetsPanelController, new UIEvent() { name = "apply asset", message = index.ToString(), type = UIEventType.ToolButton }); },
            (index) => { eventDispatcher.Raise(assetsPanelController, new UIEvent() { name = "remove asset", message = index.ToString(), type = UIEventType.ToolButton }); }
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
            eventDispatcher.Raise(this, new UIEvent() { name = "select grid map", message = gridName, type = UIEventType.Hierarchy });

        eventDispatcher.eventListener += hierarchyPanelController.EventListener;

        // simulation panel
        root.Q<Button>("play_pause").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "play pause", message = "", type = UIEventType.Simulation });
        root.Q<Button>("fast").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "fast", message = "", type = UIEventType.Simulation });
        root.Q<Button>("slow").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "slow", message = "", type = UIEventType.Simulation });
        root.Q<Button>("stop").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "stop", message = "", type = UIEventType.Simulation });

        // id panel
        var idPanelCtr = GetComponent<IdPanelController>();
        var idPanel = root.Q<VisualElement>("IdPanel");
        idPanelCtr.Init(idPanel, (containerId, childrenId) =>
            {
                eventDispatcher.Raise(this, new UIEvent()
                {
                    name = "container id",
                    message = $"{{\"containerId\":\"{containerId}\",\"childrenId\":\"{childrenId}\"}}",
                    type = UIEventType.IndoorSimData
                });
            });
        eventDispatcher.eventListener += idPanelCtr.EventListener;
        idPanel.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "id panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        idPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "id panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });

        // view panel
        var viewPanelController = GetComponent<ViewPanelController>();
        var viewPanel = root.Q<VisualElement>("ViewPanel");
        viewPanelController.Init(viewPanel,
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "enable", type = UIEventType.ViewButton }); },
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "disable", type = UIEventType.ViewButton }); });
        viewPanel.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "view panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        viewPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "view panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
    }

}
