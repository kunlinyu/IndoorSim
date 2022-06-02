using System;

public interface IAgentHW
{
    public AgentDescriptor AgentDescriptor { get; set; }
    public void ResetToInitStatus();
    public void SetControlCommand(IControlCommand command);
    public void RegisterSensorDataListener(Action<ISensorData> listener);
}
