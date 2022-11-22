using System;
using System.IO;

using UnityEngine;

public class WindowsConfigStorage : IConfigStorage
{
    public string Load(string name)
    {
        try
        {
            return File.ReadAllText(Application.persistentDataPath + name);
        }
        catch (Exception)
        {
            return "";
        }
    }

    public void Save(string name, string data)
    {
        File.WriteAllText(Application.persistentDataPath + name, data);
    }
}
