using System.Collections.Generic;
#nullable enable

public enum TaskType
{
    ActionList,
}

public abstract class Task
{
    public TaskType type;
    public double time;
}

public class ActionListTask : Task
{
    public List<AgentAction> actions;
    public ActionListTask(double time, List<AgentAction> actions)
    {
        type = TaskType.ActionList;
        this.time = time;
        this.actions = actions;
    }
}