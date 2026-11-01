using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace VariablesEcho
{
    // Separate from the active checkpoint: starting a new run preserves completed runs.
    public static class EchoCompletionHistory
    {
        [Serializable] public class Entry
        {
            public string id, ending, completedUtc;
            public int seed, hero, memory;
        }
        [Serializable] public class History { public List<Entry> entries = new List<Entry>(); }
        static string PathName { get { return Path.Combine(EchoSaveService.StorageRoot,"completion-history.json"); } }
        static History Read()
        {
            if(!File.Exists(PathName))return new History();
            var history=JsonUtility.FromJson<History>(File.ReadAllText(PathName));
            if(history==null || history.entries==null)throw new InvalidDataException("通关档案损坏");
            return history;
        }
        public static int Count { get { return Read().entries.Count; } }
        public static int Record(ChapterSave run)
        {
            var history=Read();
            string id=string.IsNullOrEmpty(run.runId)?"legacy-"+run.seed+"-"+run.heroVariant:run.runId;
            if(history.entries.Exists(e=>e.id==id))return history.entries.Count;
            history.entries.Add(new Entry {id=id,ending=run.ending,seed=run.seed,hero=run.heroVariant,memory=run.memory,completedUtc=DateTime.UtcNow.ToString("o")});
            Directory.CreateDirectory(EchoSaveService.StorageRoot);
            File.WriteAllText(PathName+".tmp",JsonUtility.ToJson(history,true));
            if(File.Exists(PathName))File.Replace(PathName+".tmp",PathName,PathName+".bak");
            else File.Move(PathName+".tmp",PathName);
            return history.entries.Count;
        }
    }
}
