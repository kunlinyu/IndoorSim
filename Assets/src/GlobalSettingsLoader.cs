using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GlobalSettingsLoader : MonoBehaviour
{
    public GlobalSettings Settings { get; private set; } = new();
    private readonly string filename = "settings.json";

    private readonly IConfigStorage configStorage = new ConfigStorage();

    public void Save()
    {
        configStorage.Save(filename, Settings.Serialize());
    }

    public void Load()
    {
        Settings = Settings.Deserialize(configStorage.Load(filename));
    }

    public void ResetToDefault()
    {
        Settings = new();
        Save();
    }

    void Start()
    {
        Load();
    }
}