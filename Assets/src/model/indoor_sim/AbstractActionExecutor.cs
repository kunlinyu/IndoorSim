using System;

#nullable enable

public enum ActionExecutorStatus
{
    Idle,
    Executing,
}

public abstract class AbstractActionExecutor : IExecutor<AgentAction, object, object, ActionExecutorStatus>
{
    ActionExecutorStatus status = ActionExecutorStatus.Idle;
    protected IAgentHW hw;

    protected AgentAction? goal;
    protected Action<AgentAction, object>? OnFinish;
    protected Action<AgentAction, object>? OnGiveUp;
    protected object goalMutex = new object();

    protected bool pause = false;

    public AbstractActionExecutor(IAgentHW hw)
    {
        this.hw = hw;
    }

    public abstract void Update();

    public ActionExecutorStatus Status() => status;

    public bool SetGoal(AgentAction goal, Action<AgentAction, object> OnFinish, Action<AgentAction, object> OnGiveUp)
    {
        if (!Accept(goal)) return false;

        lock (this)
        {
            this.goal = goal;
            this.OnFinish = OnFinish;
            this.OnGiveUp = OnGiveUp;
        }

        return true;
    }

    public bool Accept(AgentAction goal) => true;
    public object FeedBack() => null;
    public void Pause() => pause = true;
    public void Continue() => pause = false;
    public void Abort()
    {
        goal = null;
        OnFinish = null;
        OnGiveUp = null;
    }
    public void AbortAll() => Abort();
}
