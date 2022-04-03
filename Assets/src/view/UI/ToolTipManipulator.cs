using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ToolTipManipulator : Manipulator
{

    private VisualElement element;
    private VisualElement root;
    public ToolTipManipulator(VisualElement root)
    {
        this.root = root;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<MouseEnterEvent>(MouseEnter);
        target.RegisterCallback<MouseOutEvent>(MouseOut);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<MouseEnterEvent>(MouseEnter);
        target.UnregisterCallback<MouseOutEvent>(MouseOut);
    }

    private void MouseEnter(MouseEnterEvent e)
    {
        if (element == null)
        {
            element = new VisualElement();
            element.style.backgroundColor = Color.grey;
            element.style.position = Position.Absolute;
            element.style.left = this.target.worldBound.max.x;
            element.style.top = this.target.worldBound.yMin;
            var label = new Label(this.target.tooltip);
            label.style.color = Color.white;
            element.Add(label);
            root.Add(element);
        }
        element.style.visibility = Visibility.Visible;
        element.BringToFront();
    }

    private void MouseOut(MouseOutEvent e)
    {
        element.style.visibility = Visibility.Hidden;
    }
}