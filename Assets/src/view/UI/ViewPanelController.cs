using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ViewPanelController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset m_ViewBarButtonTemplate;

    public void Init(VisualElement viewBar, Action<ToolButtonData> onEnable, Action<ToolButtonData> onDisable)
    {
        List<ToolButtonData> allToolButton = new List<ToolButtonData>(Resources.LoadAll<ToolButtonData>("ViewButtonData"));
        foreach (ToolButtonData tbd in allToolButton)
        {
            TemplateContainer buttonContainer = m_ViewBarButtonTemplate.Instantiate();
            Button button = buttonContainer.Q<Button>("ToolBarButton");
            button.text = "";
            button.style.backgroundImage = new StyleBackground(tbd.m_PortraitImage);
            button.tooltip = tbd.m_ToolName;
            button.AddManipulator(new ToolTipManipulator(viewBar));
            button.AddToClassList("enable");
            button.clicked += () =>
            {
                button.ToggleInClassList("enable");
                if (button.ClassListContains("enable"))
                    onEnable?.Invoke(tbd);
                else
                    onDisable?.Invoke(tbd);
                viewBar.Focus();  // focus on viewBar to release focus on button
            };
            button.tabIndex = -1;
            viewBar.Add(buttonContainer);
        }
    }
}
