using System.Text.Json.Serialization; using System.Collections.ObjectModel;

namespace DataMaker.Refactored.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(IntField), "int")]
[JsonDerivedType(typeof(FloatField), "float")]
[JsonDerivedType(typeof(StringField), "string")]
[JsonDerivedType(typeof(ArrayField), "array")]
[JsonDerivedType(typeof(TreeField), "tree")]
[JsonDerivedType(typeof(GraphField), "graph")]
public abstract class FieldConfig
{
    public string Name { get; set; } = "field";
}
public sealed class IntField : FieldConfig { public string Min { get; set; } = "1"; public string Max { get; set; } = "100"; }
public sealed class FloatField : FieldConfig { public string Min { get; set; } = "0"; public string Max { get; set; } = "1"; public int Precision { get; set; } = 2; }
public enum StringPattern { Random, Palindrome, Periodic, Same }
public enum CharsetPreset { None, Digits, Lowercase, Uppercase, Letters, DigitsAndLetters }
public sealed class StringField : FieldConfig { public string? MinLength { get; set; } public string? MaxLength { get; set; } public string Length { get; set; } = "10"; public string Charset { get; set; } = ""; public CharsetPreset CharsetPreset { get; set; } public StringPattern Pattern { get; set; } }
public enum ArrayElementType { Int, Float, String }
public enum ArraySeparator { Space, NewLine }
public enum ArraySortOrder { None, Ascending, Descending }
public enum ArrayPattern { Random, NonDecreasing, StrictIncreasing, NonIncreasing, StrictDecreasing, Same }
public sealed class ArrayField : FieldConfig { public CharsetPreset CharsetPreset { get; set; } = CharsetPreset.Lowercase; public ArraySeparator Separator { get; set; } = ArraySeparator.Space; public string StringMinLength { get; set; } = "1"; public string StringMaxLength { get; set; } = "10"; public ArrayPattern Pattern { get; set; } = ArrayPattern.Random; public string Length { get; set; } = "10"; public ArrayElementType ElementType { get; set; } public string Min { get; set; } = "1"; public string Max { get; set; } = "100"; public int Precision { get; set; } = 2; public string Charset { get; set; } = "abcdefghijklmnopqrstuvwxyz"; public bool Unique { get; set; } public ArraySortOrder SortOrder { get; set; } }
public enum TreeShape { Random, Chain, Star, Binary }
public sealed class TreeField : FieldConfig { public string Nodes { get; set; } = "10"; public TreeShape Shape { get; set; } public int IndexStart { get; set; } = 1; public int Root { get; set; } = 1; public bool Weighted { get; set; } public string WeightMin { get; set; } = "1"; public string WeightMax { get; set; } = "100"; }
public enum GraphSpecialType { Random, Chain, Flower, BinaryTree, Grid, Sparse, Dense }
public sealed class GraphField : FieldConfig { public double Density { get; set; } = 0.2; public GraphSpecialType Shape { get; set; } = GraphSpecialType.Random; public int IndexStart { get; set; } = 1; public string Nodes { get; set; } = "10"; public string Edges { get; set; } = "20"; public bool Directed { get; set; } public bool Connected { get; set; } = true; public bool Dag { get; set; } public bool Bipartite { get; set; } public int LeftPartSize { get; set; } public bool AllowSelfLoops { get; set; } public bool AllowMultiEdges { get; set; } public bool Weighted { get; set; } public string WeightMin { get; set; } = "1"; public string WeightMax { get; set; } = "100"; }
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
[JsonDerivedType(typeof(IntegerFieldOverride), "int")]
[JsonDerivedType(typeof(FloatFieldOverride), "float")]
[JsonDerivedType(typeof(StringFieldOverride), "string")]
[JsonDerivedType(typeof(ArrayFieldOverride), "array")]
[JsonDerivedType(typeof(TreeFieldOverride), "tree")]
[JsonDerivedType(typeof(GraphFieldOverride), "graph")]
public abstract class FieldOverrideBase { public bool IsEnabled { get; set; } = true; }
public sealed class IntegerFieldOverride : FieldOverrideBase { public string? Min { get; set; } public string? Max { get; set; } }
public sealed class FloatFieldOverride : FieldOverrideBase { public string? Min { get; set; } public string? Max { get; set; } public int? Precision { get; set; } }
public sealed class StringFieldOverride : FieldOverrideBase { public string? Length { get; set; } public string? MinLength { get; set; } public string? MaxLength { get; set; } public CharsetPreset? CharsetPreset { get; set; } public string? CustomCharset { get; set; } public StringPattern? Pattern { get; set; } }
public sealed class ArrayFieldOverride : FieldOverrideBase { public string? Charset { get; set; } public ArraySeparator? Separator { get; set; } public string? StringMinLength { get; set; } public string? StringMaxLength { get; set; } public int? Precision { get; set; } public ArrayPattern? Pattern { get; set; } public string? Length { get; set; } public string? Min { get; set; } public string? Max { get; set; } public ArraySortOrder? SortOrder { get; set; } public bool? Unique { get; set; } }
public sealed class TreeFieldOverride : FieldOverrideBase { public string? Nodes { get; set; } public TreeShape? Shape { get; set; } public int? Root { get; set; } public string? WeightMin { get; set; } public string? WeightMax { get; set; } }
public sealed class GraphFieldOverride : FieldOverrideBase { public double? Density { get; set; } public string? VertexCount { get; set; } public string? EdgeCount { get; set; } public GraphSpecialType? SpecialType { get; set; } public string? WeightMin { get; set; } public string? WeightMax { get; set; } }
public sealed class GroupConfig { public string Id { get; set; } = Guid.NewGuid().ToString("N"); public string Name { get; set; } = "子任务"; public string Description { get; set; } = ""; public int From { get; set; } = 1; public int To { get; set; } = 1; public int Score { get; set; } = 0; public Dictionary<string, FieldOverrideBase> TypedOverrides { get; set; } = new(); }
public enum FailurePolicy { Continue, Stop }
public sealed class ProjectConfig { public string StorageDirectory { get; set; } = ""; public FailurePolicy FailurePolicy { get; set; } = FailurePolicy.Continue; public int RetryCount { get; set; } = 0; public int Version { get; set; } = 1; public string Name { get; set; } = "未命名题目"; public int TestCount { get; set; } = 20; public int TimeLimitMs { get; set; } = 1000; public int MemoryLimitMb { get; set; } = 256; public int MaxOutputMb { get; set; } = 64; public string MingwPath { get; set; } = ""; public string SourcePath { get; set; } = ""; public string CompileArgs { get; set; } = "-O2 -std=c++17"; public string ZipPath { get; set; } = "data.zip"; public bool MultiGroup { get; set; } public int GroupCountMin { get; set; } = 1; public int GroupCountMax { get; set; } = 1; public ObservableCollection<FieldConfig> Fields { get; set; } = new(); public List<GroupConfig> Groups { get; set; } = new(); }
