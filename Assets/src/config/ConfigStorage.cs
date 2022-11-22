

public class ConfigStorage : IConfigStorage
{
    private IConfigStorage inner;

    public ConfigStorage()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        inner = new WindowsConfigStorage();
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        inner = new LinuxConfigStorage();
#elif UNITY_WEBGL
        inner = new WebGLConfigStorage;
#else
        inner = new DefaultConfigStorage;
#endif
    }

    public string Load(string name)
    {
        return inner.Load(name);
    }

    public void Save(string name, string data)
    {
        inner.Save(name, data);
    }
}
