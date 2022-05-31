using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public abstract class TaskPlanningExecutor : IExecutor<Task, object, Task?, AgentStatus>, ICapability
{
    public Task? goal = null;
    Action<Task, object>? OnFinish;
    Action<Task, object>? OnGiveUp;

    private AbstractActionPlanner planner;
    private AbstractActionExecutor actionExecutor;


    public TaskPlanningExecutor()
    {
        planner = Planner();
        actionExecutor = ActionExecutor();
    }

    protected abstract AbstractActionPlanner Planner();
    protected abstract AbstractActionExecutor ActionExecutor();

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

    public bool SetGoal(Task goal, Action<Task, object> OnFinish, Action<Task, object> OnGiveUp)
    {
        if (Status() != AgentStatus.Idle) return false;
        if (!Accept(goal)) return false;

        this.goal = goal;
        this.OnFinish = OnFinish;
        this.OnGiveUp = OnGiveUp;

        var planResult = planner.Plan(this, goal);

        planResult.ForEach(action => actionExecutor.SetGoal(action, (action, obj) => { }, (action, obj) => { }));

        return true;
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
}
