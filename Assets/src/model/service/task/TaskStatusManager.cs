using System;
using System.Linq;
using System.Collections.Generic;


public enum TaskStatus
{
    Waiting,
    Executing,
    Finished,
    GiveUp,
}

public class TaskStatusManager
{
    private readonly Dictionary<Task, TaskStatus> tasksStatus = new Dictionary<Task, TaskStatus>();

    public void Add(Task task)
    {
        lock (tasksStatus)
        {
            tasksStatus.Add(task, TaskStatus.Waiting);
        }
    }

    public void Clear(TaskStatus status)
    {
        lock (tasksStatus)
        {
            foreach (Task task in tasksStatus.Keys)
                if (tasksStatus[task] == status)
                    tasksStatus.Remove(task);
        }
    }

    public void Clear()
    {
        lock (tasksStatus)
        {
            tasksStatus.Clear();
        }
    }

    private void Transition(Task task, string name, TaskStatus to, List<TaskStatus> from)
    {
        lock (tasksStatus)
        {
            if (!tasksStatus.ContainsKey(task)) throw new ArgumentException("Can not find the task");

            TaskStatus status = tasksStatus[task];
            if (from.Count(fromStatus => fromStatus == status) == 0)
                throw new InvalidOperationException($"Can not {name} a task with status {status}");

            tasksStatus[task] = to;
        }
    }

    public void Execute(Task task)
        => Transition(task, "execute", TaskStatus.Executing, new List<TaskStatus>() { TaskStatus.Waiting, TaskStatus.GiveUp });

    public void Finish(Task task)
        => Transition(task, "finish", TaskStatus.Finished, new List<TaskStatus>() { TaskStatus.Executing });

    public void GiveUp(Task task)
        => Transition(task, "give up", TaskStatus.GiveUp, new List<TaskStatus>() { TaskStatus.Waiting, TaskStatus.Executing });

    public List<Task> Tasks(TaskStatus status)
        => tasksStatus.Keys.Where(task => tasksStatus[task] == status).ToList();




}
