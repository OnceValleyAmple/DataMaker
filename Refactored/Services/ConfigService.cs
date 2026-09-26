using System.Text.Json; using System.Text.Json.Nodes; using System.Text.Json.Serialization; using DataMaker.Refactored.Models;
namespace DataMaker.Refactored.Services;
public sealed class ConfigService
{
 readonly JsonSerializerOptions options=new(){WriteIndented=true,PropertyNameCaseInsensitive=true,Converters={new JsonStringEnumConverter()}};

 public void Save(ProjectConfig config,string path){config.Version=2;var dir=Path.GetDirectoryName(path);if(!string.IsNullOrWhiteSpace(dir))Directory.CreateDirectory(dir);File.WriteAllText(path,JsonSerializer.Serialize(config,options));}

 public ProjectConfig Load(string path)
 {
  var json=File.ReadAllText(path);
  json=SanitizeLegacyTypedOverrides(json);
  ProjectConfig config;
  try{config=JsonSerializer.Deserialize<ProjectConfig>(json,options)??new();}
  catch(NotSupportedException ex){throw new NotSupportedException($"配置文件里的 Subtask 覆盖值格式无法识别：{ex.Message}",ex);}
  MigrateLegacyOverrides(json,config);
  if(config.Version<2)Migrate(config);
  return config;
 }

 /// <summary>
 /// FieldOverrideBase 启用多态序列化之后，TypedOverrides 的每个值都必须带 "$type" 判别属性，
 /// 否则反序列化会抛 NotSupportedException，整份配置直接打不开。
 /// 这里把早期版本写出的、没有判别属性的覆盖值转成 key=value 文本，
 /// 反序列化后再由 OverrideTextService 还原成强类型对象，保证旧配置可以继续加载。
 /// </summary>
 static string SanitizeLegacyTypedOverrides(string json)
 {
  try
  {
   var root=JsonNode.Parse(json);
   if(root is not JsonObject rootObject) return json;
   if(rootObject["Groups"] is not JsonArray groups) return json;
   var changed=false;
   foreach(var groupNode in groups)
   {
    if(groupNode is not JsonObject group) continue;
    if(group["TypedOverrides"] is not JsonObject overrides) continue;
    foreach(var key in overrides.Select(x=>x.Key).ToList())
    {
     var value=overrides[key];
     if(value is not JsonObject valueObject) continue;
     if(valueObject.ContainsKey("$type")) continue;
     var text=string.Join(";",valueObject.Where(x=>x.Value is not null).Select(x=>$"{x.Key}={x.Value!.ToJsonString()}"));
     overrides[key]=text;
     changed=true;
    }
   }
   return changed?rootObject.ToJsonString():json;
  }
  catch(JsonException){return json;}
 }

 void MigrateLegacyOverrides(string json,ProjectConfig c){try{using var doc=JsonDocument.Parse(json);if(!doc.RootElement.TryGetProperty("Groups",out var groups)||groups.ValueKind!=JsonValueKind.Array)return;for(var i=0;i<Math.Min(groups.GetArrayLength(),c.Groups.Count);i++){var ge=groups[i];if(!ge.TryGetProperty("Overrides",out var ovs)||ovs.ValueKind!=JsonValueKind.Object)continue;foreach(var field in ovs.EnumerateObject()){var values=field.Value.EnumerateObject().ToDictionary(x=>x.Name,x=>x.Value.GetString());var target=c.Fields.FirstOrDefault(x=>x.Name==field.Name);if(target is null)continue;var text=string.Join(";",values.Where(x=>x.Value is not null).Select(x=>$"{x.Key}={x.Value}"));var parsed=OverrideTextService.Parse(target,text);if(parsed is not null)c.Groups[i].TypedOverrides[field.Name]=parsed;}}}catch(JsonException){}}

 void Migrate(ProjectConfig c){c.Version=2;c.TestCount=Math.Max(1,c.TestCount);c.TimeLimitMs=Math.Max(1,c.TimeLimitMs);c.MemoryLimitMb=Math.Max(0,c.MemoryLimitMb);foreach(var f in c.Fields){if(f is ArrayField a){a.Length=string.IsNullOrWhiteSpace(a.Length)?"1":a.Length;}if(f is StringField s){s.Charset=string.IsNullOrEmpty(s.Charset)?"abcdefghijklmnopqrstuvwxyz":s.Charset;}}}
}
