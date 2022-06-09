using System;
using System.Collections.Generic;

#nullable enable

public interface IExecutor<GoalType, ResultType, FeedbackType, StatusType>
{
    public StatusType Status();
    public void SetGoal(GoalType goal, Action<GoalType, ResultType> OnFinish, Action<GoalType, ResultType> OnGiveUp);
    public bool Execute(GoalType goal, out ResultType result);
    public bool Accept(GoalType goal);
    public FeedbackType FeedBack();
    public void Pause();
    public void Continue();
    public void Abort();
    public void AbortAll();

}

public interface IBlockExecutor<GoalType, ResultType>
{
    public bool Execute(GoalType goal, ref bool cancel, out ResultType result);
    public bool Accept(GoalType goal);
}

public interface IAsyncExecutor<GoalType, ResultType, FeedbackType, StatusType>  // Agent
{
    public StatusType Status();
    public void SetGoal(GoalType goal, Action<GoalType, ResultType> OnFinish, Action<GoalType, ResultType> OnGiveUp);
    public bool Accept(GoalType goal);
    public FeedbackType FeedBack();
}

public interface IGroupExecutor<GoalType, ResultType, StatusType>
{
    public StatusType Status();
    public bool Accept(GoalType goal);
    public void SetGoalGroup(List<GoalType> goals, Action<GoalType, ResultType> OnEachFinish, Action OnAllFinish, Action<GoalType, ResultType> OnAnyGiveUp);
    public int WaitingCount();
    public void Pause();
    public void Continue();
    public void AbortCurrent();
    public void AbortRemain();
    public void AbortAll();

}

// Agent:    10min  Async Group

// Action:   60s    Block
// Motion:   10s    Group
// Actuator: 10ms   Block