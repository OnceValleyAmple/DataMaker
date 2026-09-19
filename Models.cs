using System.Text.Json.Serialization;

namespace DataMaker;

public enum FieldType { Int, Float, String, Array, Tree, Graph }
public sealed class FieldConfig
{
    public string Name { get; set; } = "field";
    public FieldType Type { get; set; } = FieldType.Int;
    public string Min { get; set; } = "1";
    public string Max { get; set; } = "100";
    public string Length { get; set; } = "1";
    public string ElementType { get; set; } = "int";
    public string Charset { get; set; } = "abcdefghijklmnopqrstuvwxyz";
    public string StringPattern { get; set; } = "random";
    public string Output { get; set; } = "line";
    public string Structure { get; set; } = "random";
    public bool Weighted { get; set; }
    public int Precision { get; set; } = 2;
    public bool Unique { get; set; }
    public string SortOrder { get; set; } = "none";
    public bool Directed { get; set; }
    public bool Connected { get; set; } = true;
    public bool AllowSelfLoop { get; set; }
    public bool AllowMultiEdge { get; set; }
    public bool Bipartite { get; set; }
    public int LeftPartSize { get; set; }
}
public sealed class GroupOverride { public int From { get; set; } = 1; public int To { get; set; } = 5; public Dictionary<string, string> Overrides { get; set; } = new(); [JsonIgnore] public string Display => $"{From}-{To}: {string.Join(", ", Overrides.Select(x => x.Key + "=" + x.Value))}"; }
public sealed class ProjectConfig
{
    public string TemplateName { get; set; } = "未命名题目";
    public string MingwPath { get; set; } = "";
    public string StandardPath { get; set; } = "";
    public string CompileArgs { get; set; } = "-O2 -std=c++17";
    public int TimeLimit { get; set; } = 1000;
    public int MemoryLimit { get; set; } = 256;
    public int TestCount { get; set; } = 20;
    public bool MultiGroupMode { get; set; }
    public int GroupCountMin { get; set; } = 1;
    public int GroupCountMax { get; set; } = 5;
    public List<FieldConfig> Fields { get; set; } = new();
    public List<GroupOverride> Groups { get; set; } = new();
    public string OutputDirectory { get; set; } = "";
    public string ZipName { get; set; } = "data.zip";
}
