using System;
using System.Collections.Generic;
using Newtonsoft.Json;
#nullable enable

[Serializable]
public class InstructionHistory<InstructionType>
{
    [JsonPropertyAttribute] private Stack<List<InstructionType>> history = new Stack<List<InstructionType>>();
    [JsonPropertyAttribute] private Stack<List<InstructionType>> future = new Stack<List<InstructionType>>();
    [JsonPropertyAttribute] private List<string> snapShots = new List<string>();

    [JsonIgnore] public Func<string> getSnapshot;
    [JsonIgnore]
    public Func<string> GetSnapshot
    {
        set
        {
            getSnapshot = value;
            if (snapShots.Count == 0)
                snapShots.Add(getSnapshot.Invoke());
        }
    }

    [JsonIgnore] private List<InstructionType>? uncommittedInstruction = null;
    [JsonIgnore] public int reEntryLevel { get; private set; } = 0;

    [JsonIgnore] public bool InSession => reEntryLevel > 0;

#pragma warning disable CS8618
    public InstructionHistory() { }  // for deserialize only
#pragma warning restore CS8618

    public void SessionStart()
    {
        if (reEntryLevel == 0)
            uncommittedInstruction = new List<InstructionType>();
        reEntryLevel += 1;
    }

    public void SessionCommit()
    {
        reEntryLevel -= 1;
        if (reEntryLevel == 0)
        {
            if (uncommittedInstruction!.Count == 0)
            {
                Console.WriteLine("Do nothing before commit.");
            }
            else
            {
                history.Push(uncommittedInstruction);
                future.Clear();
                uncommittedInstruction = null;
                while (snapShots.Count > history.Count) snapShots.RemoveAt(snapShots.Count - 1);
                string? snapShot = getSnapshot?.Invoke();
                if (snapShot != null)
                    snapShots.Add(snapShot);
            }
        }

    }

    public void DoStep(InstructionType instruction)
    {
        if (uncommittedInstruction == null)
            throw new InvalidCastException("Can not do anything before session start.");
        uncommittedInstruction.Add(instruction);
    }

    public void DoCommit(InstructionType instruction)
    {
        SessionStart();
        DoStep(instruction);
        SessionCommit();
    }

    public List<InstructionType> Undo(out string snapShot)
    {
        if (uncommittedInstruction != null)
            throw new InvalidOperationException("There are some uncommitted instruction. Should not undo.");
        Console.WriteLine("undo history: " + history.Count + " future: " + future.Count);
        if (history.Count > 0)
        {
            var last = history.Peek();
            history.Pop();
            future.Push(last);
            snapShot = history.Count < snapShots.Count ? snapShots[history.Count] : "";
            return last;
        }
        else
        {
            snapShot = "";
            return new List<InstructionType>();
        }
    }

    public List<InstructionType> Redo(out string snapShot)
    {
        if (uncommittedInstruction != null)
            throw new InvalidOperationException("There are some uncommitted instruction. Should not redo.");
        Console.WriteLine("redo history: " + history.Count + " future: " + future.Count);
        if (future.Count > 0)
        {
            var next = future.Peek();
            future.Pop();
            history.Push(next);
            snapShot = history.Count < snapShots.Count ? snapShots[history.Count] : "";
            return next;
        }
        else
        {
            snapShot = "";
            return new List<InstructionType>();
        }
    }

    public void Uuundo()
    {
        while (history.Count > 0) Undo(out var snapShot);
    }

    public void Clear()
    {
        history.Clear();
        future.Clear();
        snapShots.Clear();
    }
}
