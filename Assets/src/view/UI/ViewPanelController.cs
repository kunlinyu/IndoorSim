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
        List<ToolButtonData> allToolButton = new List<ToolButtonData>(Resources.LoadAll<ToolButtonData>("ViewButtonData"));
        foreach (ToolButtonData tbd in allToolButton)
        {
            TemplateContainer buttonContainer = m_ViewBarButtonTemplate.Instantiate();
            Button button = buttonContainer.Q<Button>("ToolBarButton");
            button.text = "";
            button.style.backgroundImage = new StyleBackground(tbd.m_PortraitImage);
            button.tooltip = tbd.m_ToolName;
            button.AddManipulator(new ToolTipManipulator(rootUIDocument.rootVisualElement));
            button.AddToClassList("enable");
            button.clicked += () =>
            {
                button.ToggleInClassList("enable");
                if (button.ClassListContains("enable"))
                    eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "enable", type = UIEventType.ViewButton });
                else
                    eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "disable", type = UIEventType.ViewButton });
                viewPanel.Focus();  // focus on viewBar to release focus on button
            };
            button.tabIndex = -1;
            viewPanel.Add(buttonContainer);
        }
        viewPanel.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(this, new UIEvent() { name = "view panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        viewPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(this, new UIEvent() { name = "view panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
    }
}
