using System;

using UnityEngine;

#nullable enable

public class TaskPlanningAgent : IAgent
{
    public Task? goal = null;
    Action<Task, object>? OnFinish;
    Action<Task, object>? OnGiveUp;

    private AbstractActionPlanner planner;
    private IExecutor<AgentAction, object?, object?, ActionExecutorStatus> actionExecutor;


    public TaskPlanningAgent(AbstractActionPlanner planner, IExecutor<AgentAction, object?, object?, ActionExecutorStatus> actionExecutor)
    {
        this.planner = planner;
        this.actionExecutor = actionExecutor;
    }

    public AgentStatus Status()
    {
        switch (actionExecutor.Status())
        {
            case ActionExecutorStatus.Idle:
                return AgentStatus.Idle;
            case ActionExecutorStatus.Executing:
                return AgentStatus.Running;
            default:
                throw new Exception("unknown action executor status");
        }
    }

    public void SetGoal(Task goal, Action<Task, object> OnFinish, Action<Task, object> OnGiveUp)
    {
        if (Status() != AgentStatus.Idle) throw new InvalidOperationException("another task is running");
        if (!Accept(goal)) throw new ArgumentException("we don't accept the task");

        this.goal = goal;
        this.OnFinish = OnFinish;
        this.OnGiveUp = OnGiveUp;

        var planResult = planner.Plan(this, goal);

        planResult.ForEach(action => actionExecutor.SetGoal(action, (action, obj) => { }, (action, obj) => { }));
    }

    public bool Accept(Task goal) => true;

    public void Pause() => actionExecutor.Pause();
    public void Continue() => actionExecutor.Continue();
    public Task? FeedBack() => goal;

    public void Abort()
    {
        actionExecutor.Abort();
        goal = null;
        OnFinish = null;
        OnGiveUp = null;
    }

    public void AbortAll()
    {
        Abort();
    }

    public Capability Capability()
    {
        return new Capability();
    }

    public bool Execute(Task goal, out object? result)
    {
        if (Status() != AgentStatus.Idle) throw new InvalidOperationException("another task is running");
        if (!Accept(goal)) throw new ArgumentException("we don't accept the task");

        this.goal = goal;

        var planResult = planner.Plan(this, goal);

        result = null;
        foreach (var action in planResult)
        {
            bool ret = actionExecutor.Execute(action, out var res);
            if (!ret) return false;
        }

        return true;
    }
}
