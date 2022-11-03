using System.IO;

#nullable enable

public interface IExporter
{
    public string Name { get;  }
    public string DefaultStreamName { get; }
    public bool CanIncludeFull { get; }
    public void Load(IndoorSimData indoorSimData);
    public bool Translate(string layerName);
    public string Export(string softwareVersion, bool includeFull);
    public void Export(Stream stream);
    public void Reset();
}
