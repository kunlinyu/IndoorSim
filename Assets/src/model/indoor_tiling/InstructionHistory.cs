using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
#nullable enable

[Serializable]
public class InstructionHistory
{
    [JsonPropertyAttribute] private Stack<List<ReducedInstruction>> history = new Stack<List<ReducedInstruction>>();
    [JsonPropertyAttribute] private Stack<List<ReducedInstruction>> future = new Stack<List<ReducedInstruction>>();

    [JsonIgnore] private List<ReducedInstruction>? uncommittedInstruction = null;
    [JsonIgnore] private int reEntryLevel = 0;

    public bool IgnoreDo { get; set; } = false;

    public void SessionStart()
    {
        if (!IgnoreDo)
        {
            if (reEntryLevel == 0)
                uncommittedInstruction = new List<ReducedInstruction>();
            reEntryLevel += 1;
        }
    }

    public void SessionCommit()
    {
        if (!IgnoreDo)
        {
            reEntryLevel -= 1;
            if (reEntryLevel == 0)
            {
                if (uncommittedInstruction!.Count == 0)
                {
                    Debug.LogWarning("Do nothing before commit.");
                }
                else
                {
                    history.Push(uncommittedInstruction);
                    future.Clear();
                    uncommittedInstruction = null;
                }
            }

        }
    }

    public void DoStep(ReducedInstruction instruction)
    {
        if (!IgnoreDo)
        {
            if (uncommittedInstruction == null)
                throw new InvalidCastException("Can not do anything before session start.");
            uncommittedInstruction.Add(instruction);
        }
    }

    public void DoCommit(ReducedInstruction instruction)
    {
        if (!IgnoreDo)
        {
            SessionStart();
            DoStep(instruction);
            SessionCommit();
        }
    }

    public List<ReducedInstruction> Undo()
    {
        if (uncommittedInstruction != null)
            throw new InvalidOperationException("There are some uncommitted instruction. Should not undo.");
        Debug.Log("undo history: " + history.Count + " future: " + future.Count);
        if (history.Count > 0)
        {
            var last = history.Peek();
            history.Pop();
            future.Push(last);
            return last;
        }
        else
        {
            return new List<ReducedInstruction>();
        }
    }

    public List<ReducedInstruction> Redo()
    {
        if (uncommittedInstruction != null)
            throw new InvalidOperationException("There are some uncommitted instruction. Should not redo.");
        Debug.Log("redo history: " + history.Count + " future: " + future.Count);
        if (future.Count > 0)
        {
            var next = future.Peek();
            future.Pop();
            history.Push(next);
            return next;
        }
        else
        {
            return new List<ReducedInstruction>();
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
