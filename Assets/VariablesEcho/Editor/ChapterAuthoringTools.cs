using System.Linq;
using System.Collections.Generic;
using CCGKit;
using VariablesEcho;
using UnityEditor;
using UnityEngine;
namespace VariablesEchoEditor
{
    public static class ChapterAuthoringTools
    {
        [MenuItem("Variables Echo/Chapter One/Clone Selected Card As New")]
        public static void CloneCard()
        {
            var original=Selection.activeObject as CardTemplate;
            if(original==null){Debug.LogWarning("Select a CardTemplate asset first.");return;}
            var data=AssetDatabase.LoadAssetAtPath<EchoChapterData>(ChapterOneBuilder.DataPath);
            var copy=Object.Instantiate(original);
            copy.Id=data.cards.Max(c=>c.Id)+1;copy.CardId="custom-"+System.Guid.NewGuid().ToString("N");copy.Name=original.Name+" Custom";copy.Upgrade=null;
            copy.Effects=new List<Effect>();
            string path=AssetDatabase.GenerateUniqueAssetPath(ChapterOneBuilder.Root+"/CustomCard.asset");
            AssetDatabase.CreateAsset(copy,path);
            foreach(var effect in original.Effects){var clone=Object.Instantiate(effect);AssetDatabase.AddObjectToAsset(clone,copy);copy.Effects.Add(clone);}
            data.cards.Add(copy);data.rewards.Add(copy);
            EditorUtility.SetDirty(copy);EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();Selection.activeObject=copy;EditorGUIUtility.PingObject(copy);
            Debug.Log("Independent effects copied; new card added to chapter database and reward pool: "+path);
        }
        [MenuItem("Variables Echo/Chapter One/Open Start Scene")]
        public static void Open()
        {
            if(UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ChapterOneBuilder.Root+"/ChapterOne.unity");
        }
    }
}
