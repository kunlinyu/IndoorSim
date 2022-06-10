using System;

#nullable enable

public interface IActuatorSensor
{
    public AbstractMotionExecutor? motionExecutor { get; set;  }
    public AgentDescriptor AgentDescriptor { get; set; }
    public void ResetToInitStatus();
    public void Execute(IActuatorCommand command);
    public void RegisterSensorDataListener(Action<ISensorData> listener);
}
