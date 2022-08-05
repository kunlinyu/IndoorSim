
using UnityEngine;
using UnityEngine.UIElements;

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
            { eventDispatcher.Raise(idPanel, new UIEvent() { name = "id panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        idPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(idPanel, new UIEvent() { name = "id panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
    }

}
