using System.Collections.Generic;
#nullable enable

public interface AbstractActionPlanner
{
    public List<AgentAction> Plan(ICapability cap, Task task);
    public bool Planable(ICapability cap, Task task);

}
public class DummyPlanner : AbstractActionPlanner
{
    public List<AgentAction> Plan(ICapability cap, Task task)
        => ((ActionListTask)task).actions;

    public bool Planable(ICapability cap, Task task)
        => task.type == TaskType.ActionList;
}