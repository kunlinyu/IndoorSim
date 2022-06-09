#nullable enable
public enum ActionType
{
    MoveToContainer,
    MoveToCoor,
}

public class AgentAction
{
    public ActionType type;
}

public class ActionMoveToContainer : AgentAction
{
    public string id;

    public ActionMoveToContainer(string id)
    {
        type = ActionType.MoveToContainer;
        this.id = id;
    }
}

public class ActionMoveToCoor : AgentAction
{
    public double x;
    public double y;

    public ActionMoveToCoor(double x, double y)
    {
        type = ActionType.MoveToCoor;
        this.x = x;
        this.y = y;
    }
}