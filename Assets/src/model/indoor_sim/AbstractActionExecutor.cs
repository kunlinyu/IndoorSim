#nullable enable

public enum ActionExecutorStatus
{
    Idle,
    Executing,
}

public abstract class AbstractActionExecutor : IBlockExecutor<AgentAction, object?>
{
    protected ActionExecutorStatus status = ActionExecutorStatus.Idle;

    protected AbstractMotionExecutor motionExe;
    public AbstractActionExecutor(AbstractMotionExecutor me)
    {
        this.motionExe = me;
    }

    public abstract bool Accept(AgentAction goal);

    public abstract bool Execute(AgentAction goal, ref bool cancel, out object? result);
}

