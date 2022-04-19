using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
#nullable enable

[Serializable]
public class InstructionHistory
{
    [JsonPropertyAttribute] private Stack<ReducedInstruction> history = new Stack<ReducedInstruction>();
    [JsonPropertyAttribute] private Stack<ReducedInstruction> future = new Stack<ReducedInstruction>();

    public bool IgnoreDo { get; set; } = false;

    public void Do(ReducedInstruction instruction)
    {
        if (!IgnoreDo)
        {
            Debug.Log("do history: " + history.Count + " future: " + future.Count);
            history.Push(instruction);
            future.Clear();
        }
    }

    public ReducedInstruction? Undo()
    {
        Debug.Log("undo history: " + history.Count + " future: " + future.Count);
        if (history.Count > 0)
        {
            ReducedInstruction last = history.Peek();
            history.Pop();
            future.Push(last);
            return last;
        }
        else
        {
            return null;
        }
    }

    public ReducedInstruction? Redo()
    {
        Debug.Log("redo history: " + history.Count + " future: " + future.Count);
        if (future.Count > 0)
        {
            ReducedInstruction next = future.Peek();
            future.Pop();
            history.Push(next);
            return next;
        }
        else
        {
            return null;
        }
    }

    public void Uuundo()
    {
        while (history.Count > 0) Undo();
    }

    public void Clear()
    {
        history.Clear();
        future.Clear();
    }
}
