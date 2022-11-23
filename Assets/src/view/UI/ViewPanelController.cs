using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ViewPanelController : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    public UIDocument rootUIDocument;
    [SerializeField] private VisualTreeAsset m_ViewBarButtonTemplate;

    void Start()
    {
        VisualElement viewPanel = rootUIDocument.rootVisualElement.Q<VisualElement>("ViewPanel");

        viewPanel.Query<Button>().ForEach(button =>
        {
            button.AddManipulator(new ToolTipManipulator(rootUIDocument.rootVisualElement));
            button.AddToClassList("enable");
            button.clicked += () =>
            {
                button.ToggleInClassList("enable");
                if (button.ClassListContains("enable"))
                    eventDispatcher.Raise(this, new UIEvent() { name = button.tooltip, message = "enable", type = UIEventType.ViewButton });
                else
                    eventDispatcher.Raise(this, new UIEvent() { name = button.tooltip, message = "disable", type = UIEventType.ViewButton });
                viewPanel.Focus();  // focus on viewBar to release focus on button
            };
            button.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(this, new UIEvent() { name = "view panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
            button.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(this, new UIEvent() { name = "view panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
        });

    }
}
