using UnityEngine;
using UnityEngine.UIElements;

public class SimulationPlayerPanel : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    public UIDocument rootUIDocument;

    void Start()
    {
        VisualElement root = rootUIDocument.rootVisualElement;
        root.Q<Button>("play_pause").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "play pause", message = "", type = UIEventType.Simulation });
        root.Q<Button>("fast").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "fast", message = "", type = UIEventType.Simulation });
        root.Q<Button>("slow").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "slow", message = "", type = UIEventType.Simulation });
        root.Q<Button>("stop").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "stop", message = "", type = UIEventType.Simulation });

        VisualElement simulationPanel = root.Q<VisualElement>("SimulationPanel");
        simulationPanel.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(this, new UIEvent() { name = "sim panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        simulationPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(this, new UIEvent() { name = "sim panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
    }
}
