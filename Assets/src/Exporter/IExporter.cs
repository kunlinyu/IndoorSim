using System.IO;

#nullable enable

public interface IExporter
{
    public string name { get;  }
    public string defaultStreamName { get; }
    public bool canIncludeFull { get; }
    public void Load(IndoorSimData indoorSimData);
    public void Translate(string layerName);
    public string Export(bool includeFull);
    public void Export(Stream stream);
}
