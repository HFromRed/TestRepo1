using System;
using System.Linq;
using CCGKit;
using UnityEditor;
using UnityEngine;
using VariablesEcho;

namespace VariablesEchoEditor
{
    [InitializeOnLoad]
    public static class StrengthCardsInstaller
    {
        const string Root="Assets/VariablesEcho/ChapterOne/StrengthCards";
        const string Art="Assets/VariablesEcho/Resources/StrengthCards/";
        static StrengthCardsInstaller()
        {
            EditorApplication.delayCall+=()=>
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)return;
                bool complete=StrengthRewardSet.Ids.All(id=>AssetDatabase.LoadAssetAtPath<CardTemplate>(Root+"/Card_"+id+".asset")!=null);
                string[] starterNames={"strike","defend","bash"};
                bool starterFiles=starterNames.All(n=>System.IO.File.Exists("Assets/VariablesEcho/Resources/StarterCards/"+n+".png"));
                if(complete && (!starterFiles || Enumerable.Range(0,3).All(i=>
                    AssetDatabase.LoadAssetAtPath<CardTemplate>("Assets/VariablesEcho/ChapterOne/Card_"+(201+i)+".asset").Picture==
                    AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VariablesEcho/Resources/StarterCards/"+starterNames[i]+".png"))))return;
                if(Enumerable.Range(0,10).All(i=>System.IO.File.Exists(Art+"card-"+i.ToString("00")+".png")))Install();
            };
        }
        [MenuItem("Variables Echo/Cards/Install and Validate Ten Strength Cards")]
        public static void Install()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before installing cards.");
            for(int i=0;i<10;i++)
            {
                string path=Art+"card-"+i.ToString("00")+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)throw new Exception("Missing illustration: "+path);
                importer.textureType=TextureImporterType.Sprite;
                importer.spriteImportMode=SpriteImportMode.Single;
                importer.filterMode=FilterMode.Point;
                importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.npotScale=TextureImporterNPOTScale.None;
                importer.wrapMode=TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/VariablesEcho/ChapterOne","StrengthCards");
            var data=AssetDatabase.LoadAssetAtPath<EchoChapterData>(ChapterOneBuilder.DataPath);
            StrengthRewardSet.Install(data);
            foreach(int id in StrengthRewardSet.Ids)
            {
                var card=data.FindCard(id);
                if(!AssetDatabase.Contains(card))
                {
                    AssetDatabase.CreateAsset(card,Root+"/Card_"+id+".asset");
                    foreach(var effect in card.Effects)AssetDatabase.AddObjectToAsset(effect,card);
                }
                EditorUtility.SetDirty(card);
            }
            var library=data.hero.RewardDeck;
            library.Entries.RemoveAll(e=>e.Card!=null && StrengthRewardSet.IsRemoved(e.Card.Id));
            foreach(int id in StrengthRewardSet.Ids)
                if(!library.Entries.Any(e=>e.Card!=null && e.Card.Id==id))library.Entries.Add(new CardLibraryEntry{Card=data.FindCard(id),NumCopies=1});
            EditorUtility.SetDirty(library);EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
            InstallStarterArt();
            Validate();
        }
        static void InstallStarterArt()
        {
            string[] names={"strike","defend","bash"};
            for(int i=0;i<3;i++)
            {
                string path="Assets/VariablesEcho/Resources/StarterCards/"+names[i]+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)continue;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                foreach(int id in new[]{201+i,1201+i})
                {
                    var card=AssetDatabase.LoadAssetAtPath<CardTemplate>("Assets/VariablesEcho/ChapterOne/Card_"+id+".asset");
                    if(card==null)throw new Exception("Missing starter card: "+id);
                    card.Picture=sprite;EditorUtility.SetDirty(card);
                }
            }
            AssetDatabase.SaveAssets();
        }
        public static void Validate()
        {
            var source=AssetDatabase.LoadAssetAtPath<EchoChapterData>(ChapterOneBuilder.DataPath);
            for(int act=1;act<=3;act++)
            {
                var data=EchoCampaign.Create(source,act);StrengthRewardSet.Install(data);StrengthRewardSet.Install(data);
                var cards=data.cards.Where(c=>c.Id>=7300 && c.Id<7320).ToArray();
                if(cards.Length!=10 || cards.Select(c=>c.Id).Distinct().Count()!=10)throw new Exception("Strength card IDs/count invalid");
                if(cards.Any(c=>c.Picture==null) || cards.Select(c=>c.Picture).Distinct().Count()!=10)throw new Exception("Card art must be one-to-one");
                if(data.rewards.Count(c=>c.Id>=7300 && c.Id<7320)!=10)throw new Exception("Reward pool invalid");
                if(cards.Any(c=>c.Effects.Count==0))throw new Exception("Empty effects");
                UnityEngine.Object.DestroyImmediate(data);
            }
            var save=new ChapterSave();save.deck=Enumerable.Range(7300,20).ToList();save.reward=save.deck.ToList();
            StrengthRewardSet.MigrateSave(save);
            if(save.deck.Any(StrengthRewardSet.IsRemoved)||save.reward.Any(StrengthRewardSet.IsRemoved)||save.deck.Count!=20)throw new Exception("Legacy save migration invalid");
            Debug.Log("STRENGTH_TEN_VALIDATED: 10 unique illustrations/cards; rewards in 3 chapters; idempotent install; legacy save migration.");
        }
    }
}
