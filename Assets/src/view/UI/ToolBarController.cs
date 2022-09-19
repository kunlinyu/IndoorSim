using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolBarController : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    public UIDocument rootUIDocument;
    [SerializeField] private VisualTreeAsset m_ToolBarButtonTemplate;
    private Button activeButton = null;

    private Dictionary<Button, ToolButtonData> button2ButonData = new Dictionary<Button, ToolButtonData>();

    void Start()
    {
        VisualElement toolBar = rootUIDocument.rootVisualElement.Q<VisualElement>("ToolBar");
        LoadButtons(toolBar,
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "trigger", type = UIEventType.ToolButton }); },
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "enter", type = UIEventType.ToolButton }); },
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "leave", type = UIEventType.ToolButton }); });
        toolBar.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        toolBar.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
    }
    public void LoadButtons(VisualElement toolBar, Action<ToolButtonData> onTrigger, Action<ToolButtonData> onEnter, Action<ToolButtonData> onLeave)
    {
        List<ToolButtonData> allToolButton = new List<ToolButtonData>(Resources.LoadAll<ToolButtonData>("ToolButtonData"));
        allToolButton.Sort((tbd1, tbd2) => { return tbd1.m_SortingOrder - tbd2.m_SortingOrder; });

        foreach (ToolButtonData tbd in allToolButton)
        {
            TemplateContainer buttonContainer = m_ToolBarButtonTemplate.Instantiate();
            Button button = buttonContainer.Q<Button>("ToolBarButton");

            button2ButonData[button] = tbd;

            button.text = "";
            button.style.backgroundImage = new StyleBackground(tbd.m_PortraitImage);
            button.tooltip = tbd.m_ToolName;
            button.AddManipulator(new ToolTipManipulator(toolBar));
            if (tbd.m_Class == ToolType.TriggerAndFinish)
            {
                button.clicked += () =>
                {
                    onTrigger?.Invoke(tbd);
                    if (activeButton != null)
                        onLeave?.Invoke(button2ButonData[activeButton]);
                    DisableButton(activeButton);
                    toolBar.Focus();  // focus on toolbar to release focus on button
                };
            }
            else if (tbd.m_Class == ToolType.EnterActiveMode)
            {
                button.clicked += () =>
                {
                    if (activeButton == button)
                    {
                        onLeave?.Invoke(tbd);
                        DisableButton(button);
                    }
                    else
                    {
                        onEnter?.Invoke(tbd);
                        SwitchButton(button);
                    }
                    toolBar.Focus();  // focus on toolbar to release focus on button
                };

            }
            else throw new Exception("unknow ToolType: " + tbd.m_Class);

            button.tabIndex = -1;
            toolBar.Add(buttonContainer);
        }
    }

    private void SwitchButton(Button button)
    {
        SetButtonClass(activeButton, false);
        activeButton = button;
        SetButtonClass(activeButton, true);
        Debug.Log("switch active button " + activeButton.tooltip);
    }

    private void DisableButton(Button button)
    {
        SetButtonClass(button, false);
        activeButton = null;
    }

    private void SetButtonClass(Button button, bool enable)
    {
        if (button == null) return;
        if (enable) button.AddToClassList("enable");
        else button.RemoveFromClassList("enable");
    }
}
