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

    void Start()
    {
        VisualElement toolBar = rootUIDocument.rootVisualElement.Q<VisualElement>("ToolBar");
        LoadButtons(toolBar,
            (toolName) => { eventDispatcher.Raise(this, new UIEvent() { name = toolName, message = "trigger", type = UIEventType.ToolButton }); },
            (toolName) => { eventDispatcher.Raise(this, new UIEvent() { name = toolName, message = "enter", type = UIEventType.ToolButton }); },
            (toolName) => { eventDispatcher.Raise(this, new UIEvent() { name = toolName, message = "leave", type = UIEventType.ToolButton }); });
    }
    public void LoadButtons(VisualElement toolBar, Action<string> onTrigger, Action<string> onEnter, Action<string> onLeave)
    {
        toolBar.Query<Button>().ForEach(button =>
        {
            button.AddManipulator(new ToolTipManipulator(rootUIDocument.rootVisualElement));
            button.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
            button.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
        });

        toolBar.Query<Button>("triggerButton").ForEach(button =>
        {
            button.clicked += () =>
            {
                onTrigger?.Invoke(button.tooltip);
                if (activeButton != null)
                    onLeave?.Invoke(button.tooltip);
                DisableButton(activeButton);
                toolBar.Focus();  // focus on toolbar to release focus on button
            };
        });

        toolBar.Query<Button>("enterLeaveButton").ForEach(button =>
        {
            button.clicked += () =>
            {
                if (activeButton == button)
                {
                    onLeave?.Invoke(button.tooltip);
                    DisableButton(button);
                }
                else
                {
                    onEnter?.Invoke(button.tooltip);
                    SwitchButton(button);
                }
                toolBar.Focus();  // focus on toolbar to release focus on button
            };
        });
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
