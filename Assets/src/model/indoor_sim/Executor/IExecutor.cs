using System;

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
