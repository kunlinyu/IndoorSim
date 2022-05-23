using System;
using System.Collections.Generic;
using UnityEngine;

public interface RobotHW
{
    public void SetControlCommand(IControlCommand command);
    public void RegisterSensorDataListener(Action<ISensorData> listener);
}
