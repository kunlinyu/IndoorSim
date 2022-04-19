using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolBarController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset m_ToolBarButtonTemplate;
    public UIEventDispatcher eventDispatcher;

    void Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;

        List<ToolButtonData> allToolButton = new List<ToolButtonData>();
        allToolButton.AddRange(Resources.LoadAll<ToolButtonData>("ToolButtonData"));
        allToolButton.Sort((tbd1, tbd2) => { return tbd1.m_SortingOrder - tbd2.m_SortingOrder; });
        allToolButton.Sort((tbd1, tbd2) => { return tbd1.m_Class - tbd2.m_Class; });

        VisualElement toolBar = root.Q<VisualElement>("ToolBar");

        foreach (ToolButtonData tbd in allToolButton)
        {
            TemplateContainer buttonContainer = m_ToolBarButtonTemplate.Instantiate();
            Button button = buttonContainer.Q<Button>("ToolBarButton");
            button.text = "";
            VisualElement background = new VisualElement();
            button.style.backgroundImage = new StyleBackground(tbd.m_PortraitImage);
            button.tooltip = tbd.m_ToolName;
            button.AddManipulator(new ToolTipManipulator(root));
            button.clicked += () =>
            {
                eventDispatcher.Raise(button, new UIEvent() { name = tbd.m_ToolName, type = UIEventType.ButtonClick });
            };

            toolBar.Add(buttonContainer);
        }

        toolBar.RegisterCallback<MouseEnterEvent>(e => { eventDispatcher.Raise(toolBar, new UIEvent() { name = "enter", type = UIEventType.EnterUIPanel }); });
        toolBar.RegisterCallback<MouseLeaveEvent>(e => { eventDispatcher.Raise(toolBar, new UIEvent() { name = "leave", type = UIEventType.EnterUIPanel }); });

    }

    void Update()
    {

    }
}
