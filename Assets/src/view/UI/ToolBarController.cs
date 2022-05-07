using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolBarController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset m_ToolBarButtonTemplate;

    public void LoadButtons(VisualElement toolBar, Action<Button, ToolButtonData> onClicked)
    {
        List<ToolButtonData> allToolButton = new List<ToolButtonData>(Resources.LoadAll<ToolButtonData>("ToolButtonData"));
        allToolButton.Sort((tbd1, tbd2) => { return tbd1.m_SortingOrder - tbd2.m_SortingOrder; });
        allToolButton.Sort((tbd1, tbd2) => { return tbd1.m_Class - tbd2.m_Class; });

        foreach (ToolButtonData tbd in allToolButton)
        {
            TemplateContainer buttonContainer = m_ToolBarButtonTemplate.Instantiate();
            Button button = buttonContainer.Q<Button>("ToolBarButton");
            button.text = "";
            button.style.backgroundImage = new StyleBackground(tbd.m_PortraitImage);
            button.tooltip = tbd.m_ToolName;
            button.AddManipulator(new ToolTipManipulator(toolBar));
            button.clicked += () => { onClicked(button, tbd); };
            toolBar.Add(buttonContainer);
        }
    }
}
