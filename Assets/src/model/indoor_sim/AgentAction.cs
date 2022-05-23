#nullable enable
public enum ActionType
{
    MoveToId,
    MoveToCoor,
}

public class AgentAction
{
    public ActionType type;
}

public class ActionMoveToId : AgentAction
{
    public string id;

    public ActionMoveToId(string id)
    {
        type = ActionType.MoveToId;
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