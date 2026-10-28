using System.Collections.Generic;
using System.IO;
using CCGKit;
using UnityEditor;
using UnityEngine;
using VariablesEcho;

namespace VariablesEchoEditor
{
    /// <summary>
    /// 变量残响框架的编辑器辅助工具。
    /// </summary>
    public static class EchoEditorTools
    {
        [MenuItem("Variables Echo/Create Config Asset (Resources)")]
        public static void CreateConfigAsset()
        {
            const string baseFolder = "Assets/VariablesEcho";
            const string resourcesFolder = "Assets/VariablesEcho/Resources";

            if (!AssetDatabase.IsValidFolder(resourcesFolder))
            {
                if (!AssetDatabase.IsValidFolder(baseFolder))
                    AssetDatabase.CreateFolder("Assets", "VariablesEcho");
                AssetDatabase.CreateFolder(baseFolder, "Resources");
            }

            var path = resourcesFolder + "/EchoConfig.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EchoConfig>(path);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var config = ScriptableObject.CreateInstance<EchoConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = config;
            Debug.Log($"[VariablesEcho] 已创建配置资产: {path}");
        }

        [MenuItem("Variables Echo/Fill Card IDs")]
        public static void FillCardIds()
        {
            var guids = AssetDatabase.FindAssets("t:CardTemplate");
            var filled = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardTemplate>(path);
                if (card == null)
                    continue;

                if (!string.IsNullOrEmpty(card.CardId))
                    continue;

                card.CardId = Path.GetFileNameWithoutExtension(path);
                EditorUtility.SetDirty(card);
                filled++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[VariablesEcho] 已为 {filled} 张卡牌填充 CardId。");
        }

        [MenuItem("Variables Echo/Validate Setup")]
        public static void ValidateSetup()
        {
            var seen = new Dictionary<string, string>();
            var missing = 0;
            var duplicates = 0;
            var total = 0;

            var cardGuids = AssetDatabase.FindAssets("t:CardTemplate");
            foreach (var guid in cardGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardTemplate>(path);
                if (card == null)
                    continue;

                total++;

                if (string.IsNullOrEmpty(card.CardId))
                {
                    missing++;
                    Debug.LogWarning($"[VariablesEcho] 卡牌缺少 CardId: {path}");
                    continue;
                }

                if (seen.ContainsKey(card.CardId))
                {
                    duplicates++;
                    Debug.LogWarning($"[VariablesEcho] CardId 重复: {card.CardId} ({path} / {seen[card.CardId]})");
                }
                else
                {
                    seen.Add(card.CardId, path);
                }
            }

            var enemiesWithoutProfile = 0;
            var enemyGuids = AssetDatabase.FindAssets("t:EnemyTemplate");
            foreach (var guid in enemyGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyTemplate>(path);
                if (enemy != null && enemy.EchoProfile == null)
                    enemiesWithoutProfile++;
            }

            Debug.Log(
                $"[VariablesEcho] 校验完成：卡牌 {total} 张，缺少 CardId {missing} 张，重复 {duplicates} 个；" +
                $"敌人 {enemyGuids.Length} 个，其中 {enemiesWithoutProfile} 个未配置 EchoProfile（仍可运行，但没有克制行为）。");
        }

        [MenuItem("Variables Echo/Delete Save (Debug)")]
        public static void DeleteSave()
        {
            EchoSaveService.Delete(EchoSaveService.MetaPath);
            EchoSaveService.Delete(EchoSaveService.RunPath);
            PlayerPrefs.DeleteKey("save");
            PlayerPrefs.DeleteKey("map");
            PlayerPrefs.Save();
            Debug.Log("[VariablesEcho] 已删除元进度、跑图存档与 Kit 进度。");
        }
    }
}
