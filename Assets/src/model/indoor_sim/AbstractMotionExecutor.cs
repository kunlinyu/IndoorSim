using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

#nullable enable

public enum MotionExecutorStatus
{
    Idle,
    Executing,
}

public abstract class AbstractMotionExecutor : IGroupExecutor<Motion, object?, MotionExecutorStatus>
{
    MotionExecutorStatus status = MotionExecutorStatus.Idle;
    private IActuatorSensor hw;
    private Motion? currentGoal = null;
    private Action<Motion, object?>? OnEachFinish;
    private Action<Motion, object?>? OnAnyGiveUp;
    private Action? OnAllFinish;
    private bool pause = false;

    private Queue<Motion> motionQueue = new Queue<Motion>();

    public AbstractMotionExecutor(IActuatorSensor hw)
    {
        this.hw = hw;
        this.hw.RegisterSensorDataListener(SensorDataListenerWrapper);
    }

    protected abstract IActuatorCommand SensorDataListener(ISensorData sensorData, Motion? goal);

    protected abstract bool Finish();
    protected abstract bool GiveUp();

    protected abstract IActuatorCommand PauseCommand();
    protected abstract IActuatorCommand StopCommand();

    protected void SensorDataListenerWrapper(ISensorData sensorData)
    {
        var cmd = SensorDataListener(sensorData, currentGoal);

        lock (motionQueue)
        {
            if (currentGoal == null)
            {
                if (motionQueue.Count > 0)
                    currentGoal = motionQueue.Dequeue();
                else
                    return;
            }
        }

        if (pause)
            hw.Execute(PauseCommand());
        else
            hw.Execute(cmd);

        if (Finish() || GiveUp())
        {
            hw.Execute(StopCommand());
            if (Finish())
                OnEachFinish?.Invoke(currentGoal, null);
            else
                OnAnyGiveUp?.Invoke(currentGoal, null);

            lock (motionQueue)
            {
                currentGoal = null;
                if (motionQueue.Count == 0)
                {
                    OnEachFinish = null;
                    OnAnyGiveUp = null;
                    status = MotionExecutorStatus.Idle;
                }
            }
        }
    }

    public MotionExecutorStatus Status() => status;
    public void SetGoalGroup(List<Motion> goals, Action<Motion, object?> OnEachFinish, Action OnAllFinish, Action<Motion, object?> OnAnyGiveUp)
    {
        if (goals.Any(goal => !Accept(goal))) throw new ArgumentException("un acceptable motion");
        lock (motionQueue)
        {
            goals.ForEach(goal => motionQueue.Enqueue(goal));
            status = MotionExecutorStatus.Executing;
            Debug.Log("motion executor get motions");
        }
    }

    public abstract bool Accept(Motion goal);
    public void Pause() => pause = true;
    public void Continue() => pause = false;

    public int WaitingCount()
    {
        lock (motionQueue) return motionQueue.Count;
    }

    public void AbortCurrent()
    {
        lock (motionQueue)
        {
            if (currentGoal != null) currentGoal = null;
            if (motionQueue.Count == 0)
                hw.Execute(StopCommand());
        }
    }

    public void AbortAll()
    {
        lock (motionQueue)
        {
            if (currentGoal != null) currentGoal = null;
            motionQueue.Clear();
            hw.Execute(StopCommand());
            status = MotionExecutorStatus.Idle;
        }
    }
    public void AbortRemain()
    {
        lock (motionQueue) motionQueue.Clear();
    }



}
