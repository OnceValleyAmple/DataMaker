using DataMaker.Refactored.Models;
namespace DataMaker.Refactored.Services;
public sealed class TemplateService
{ readonly ConfigService config=new(); public void Save(ProjectConfig project,string path)=>config.Save(project,path); public ProjectConfig Load(string path)=>config.Load(path); public IEnumerable<string> List(string directory){if(!Directory.Exists(directory))yield break;foreach(var file in Directory.EnumerateFiles(directory,"*.json"))yield return file;} public void Delete(string path){if(File.Exists(path))File.Delete(path);}}
