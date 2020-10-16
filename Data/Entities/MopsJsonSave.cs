using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using System;
using Discord;

namespace MopsBot.Data.Entities
{
    public static class MopsJsonSave
    {
        public static void SaveDictionary<T>(Dictionary<string, T> toSave, string name)
        {
            try
            {
                System.IO.Directory.CreateDirectory($".//mopsdata//JsonDB//{name}");
                foreach (var entity in toSave)
                {
                    using (StreamWriter stream = System.IO.File.CreateText($".//mopsdata//JsonDB//{name}//{entity.Key}.json"))
                    {
                        stream.Write(JsonConvert.SerializeObject(entity.Value));
                    }
                }
            }
            catch (Exception e)
            {
                Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error when saving {name}", e)).Wait();
            }
        }

        public static Dictionary<string, T> LoadDictionary<T>(string name)
        {
            Dictionary<string, T> toLoad = new Dictionary<string, T>();
            try
            {
                System.IO.Directory.CreateDirectory($".//JsonDB//{name}");
                foreach (var entity in new System.IO.DirectoryInfo($".//mopsdata//JsonDB//{name}").GetFiles())
                {
                    using (StreamReader stream = entity.OpenText())
                    {
                        T loaded = JsonConvert.DeserializeObject<T>(stream.ReadToEnd());
                        toLoad.Add(entity.Name.Split(".").First(), loaded);
                    }
                }
            }
            catch (Exception e)
            {
                Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error when loading {name}", e)).Wait();
            }

            return toLoad;
        }

        public static void SaveEntity(string folderName, string entityName, object entity)
        {
            try
            {
                System.IO.Directory.CreateDirectory($".//mopsdata//JsonDB//{folderName}");
                using (StreamWriter stream = System.IO.File.CreateText($".//mopsdata//JsonDB//{folderName}//{entityName}.json"))
                {
                    stream.Write(JsonConvert.SerializeObject(entity));
                }
            }
            catch (Exception e)
            {
                Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error when saving {folderName}/{entityName}", e)).Wait();
            }
        }

        public static T LoadEntity<T>(string folderName, string entityName)
        {
            try
            {
                System.IO.Directory.CreateDirectory($".//mopsdata//JsonDB//{folderName}");
                using (StreamReader stream = System.IO.File.OpenText($".//mopsdata//JsonDB//{folderName}//{entityName}.json"))
                {
                    return JsonConvert.DeserializeObject<T>(stream.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error when loading {folderName}/{entityName}", e)).Wait();
            }
            return default(T);
        }

        public static void RemoveEntity(string folderName, string entityName)
        {
            try
            {
                System.IO.File.Delete($".//mopsdata//JsonDB//{folderName}//{entityName}");
            }
            catch (Exception e)
            {
                Program.MopsLog(new LogMessage(LogSeverity.Error, "", $" error when deleting {folderName}/{entityName}", e)).Wait();
            }
        }
    }
}