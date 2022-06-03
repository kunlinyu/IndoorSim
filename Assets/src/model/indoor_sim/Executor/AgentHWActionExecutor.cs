using System;
using System.Threading;

using UnityEngine;

#nullable enable

using ActionExecutor = IExecutor<AgentAction, object?, object?, ActionExecutorStatus>;

public enum ActionExecutorStatus
{
    Idle,
    Executing,
}

public abstract class AgentHWActionExecutor : ActionExecutor
{
    ActionExecutorStatus status = ActionExecutorStatus.Idle;
    private IAgentHW hw;
    private AgentAction? goal;
    private Action<AgentAction, object?>? OnFinish;
    private Action<AgentAction, object?>? OnGiveUp;
    private bool pause = false;

    public AgentHWActionExecutor(IAgentHW hw)
    {
        this.hw = hw;
        this.hw.RegisterSensorDataListener(SensorDataListenerWrapper);
    }

    protected abstract IControlCommand SensorDataListener(ISensorData sensorData, AgentAction? goal);

    protected abstract bool Finish();
    protected abstract bool GiveUp();

    protected abstract IControlCommand PauseCommand();
    protected abstract IControlCommand StopCommand();

    protected void SensorDataListenerWrapper(ISensorData sensorData)
    {
        var cmd = SensorDataListener(sensorData, goal);

        if (goal == null) return;

        if (pause)
            hw.SetControlCommand(PauseCommand());
        else
            hw.SetControlCommand(cmd);

        if (Finish() || GiveUp())
        {
            hw.SetControlCommand(StopCommand());
            if (Finish())
                OnFinish?.Invoke(goal, null);
            else
                OnGiveUp?.Invoke(goal, null);
            goal = null;
            OnFinish = null;
            OnGiveUp = null;
            status = ActionExecutorStatus.Idle;
        }
    }

    public ActionExecutorStatus Status() => status;

    public void SetGoal(AgentAction goal, Action<AgentAction, object?> OnFinish, Action<AgentAction, object?> OnGiveUp)
    {
        if (!Accept(goal)) throw new ArgumentException("we don't accept the action");

        this.goal = goal;
        this.OnFinish = OnFinish;
        this.OnGiveUp = OnGiveUp;
        status = ActionExecutorStatus.Executing;
    }

    public bool Execute(AgentAction goal, out object? result)
    {
        result = null;
        if (!Accept(goal)) return false;

        this.goal = goal;
        status = ActionExecutorStatus.Executing;

        while (status != ActionExecutorStatus.Idle) Thread.Sleep(10);

        if (GiveUp())
            return false;
        else
            return true;
    }

    public bool Accept(AgentAction goal) => true;
    public object? FeedBack() => null;
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
