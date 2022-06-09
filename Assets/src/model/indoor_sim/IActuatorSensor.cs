using System;

public interface IActuatorSensor
{
    public AgentDescriptor AgentDescriptor { get; set; }
    public void ResetToInitStatus();
    public void Execute(IActuatorCommand command);
    public void RegisterSensorDataListener(Action<ISensorData> listener);
}
