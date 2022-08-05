
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(IdPanelController))]
public class All : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    VisualElement root;

    void Start()
    {
        // TODO: move everthing here to separate object
        root = GetComponent<UIDocument>().rootVisualElement;

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
