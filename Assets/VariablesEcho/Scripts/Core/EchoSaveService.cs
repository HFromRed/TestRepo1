using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// JSON 存档读写。元进度和跑图状态分开保存，避免跑图损坏影响长期档案。
    /// </summary>
    public static class EchoSaveService
    {
        // Development smoke tests use an isolated profile and never touch player progress.
        public static string StorageRoot
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "--chapter-smoke") >= 0)
                    return Path.Combine(Application.temporaryCachePath, "chapter-smoke");
#endif
                return Application.persistentDataPath;
            }
        }
        public static string SaveDirectory
        {
            get { return Path.Combine(StorageRoot, "variables_echo"); }
        }

        public static string MetaPath
        {
            get { return Path.Combine(SaveDirectory, "meta.json"); }
        }

        public static string RunPath
        {
            get { return Path.Combine(SaveDirectory, "run.json"); }
        }

        public static bool TryRead<T>(string path, out T value) where T : class
        {
            value = null;
            try
            {
                if (!File.Exists(path))
                    return false;

                var json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrEmpty(json))
                    return false;

                value = JsonUtility.FromJson<T>(json);
                return value != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VariablesEcho] 读取存档失败: {path}\n{e.Message}");
                return false;
            }
        }

        public static bool Write<T>(string path, T value) where T : class
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonUtility.ToJson(value, true);
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, json, Encoding.UTF8);

                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tmp, path);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[VariablesEcho] 写入存档失败: {path}\n{e.Message}");
                return false;
            }
        }

        public static void Delete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VariablesEcho] 删除存档失败: {path}\n{e.Message}");
            }
        }

        /// <summary>清除 SinglePlayerCCGKit 自己的 PlayerPrefs 进度。</summary>
        public static void ClearKitProgress()
        {
            PlayerPrefs.DeleteKey("save");
            PlayerPrefs.DeleteKey("map");
            PlayerPrefs.Save();
        }
    }
}

