using System;
using System.IO;
using System.Linq;
using CCGKit;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using VariablesEcho;
namespace VariablesEchoEditor
{
    public static class IvoryArtInstaller
    {
        const string Root="Assets/VariablesEcho/ArtV3";
        static Color Ink=new Color(.055f,.055f,.085f,.98f),Ivory=new Color(.87f,.84f,.75f),Teal=new Color(.34f,.85f,.79f),Coral=new Color(.98f,.39f,.42f);
        static T Load<T>(string p)where T:UnityEngine.Object {var a=AssetDatabase.LoadAssetAtPath<T>(p);if(a==null)throw new Exception("Missing art: "+p);return a;}
        static Sprite Image(string name,bool character=false)
        {
            var path=Root+"/"+name+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var imp=(TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.mipmapEnabled=false;imp.alphaIsTransparency=true;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=2048;imp.filterMode=FilterMode.Bilinear;imp.isReadable=true;
            var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteAlignment=(int)(character?SpriteAlignment.BottomCenter:SpriteAlignment.Center);settings.spritePixelsPerUnit=character?200:100;imp.SetTextureSettings(settings);imp.SaveAndReimport();
            return Load<Sprite>(path);
        }
        static Sprite Texture(string name,int w,int h,Func<int,int,Color> pixel,bool sliced=false)
        {
            var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);for(int y=0;y<h;y++)for(int x=0;x<w;x++)tex.SetPixel(x,y,pixel(x,y));tex.Apply();
            string path=Root+"/"+name+".png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
            var sprite=Image(name);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.spriteBorder=sliced?new Vector4(20,20,20,20):Vector4.zero;imp.SaveAndReimport();return Load<Sprite>(path);
        }
        static Sprite Panel(string name,Color line,Color fill)
        {
            return Texture(name,128,128,(x,y)=>{int d=Math.Min(Math.Min(x,y),Math.Min(127-x,127-y));if(x+y<12||x+y>242)return Color.clear;if(d<2)return Ink;if(d<4)return line;if(d<7)return Ink;if(d==8)return new Color(line.r,line.g,line.b,.4f);if(x<12&&y>90)return Teal;return fill;},true);
        }
        public static void BuildUI()
        {
            Directory.CreateDirectory(Root+"/Resources");AssetDatabase.Refresh();
            var theme=AssetDatabase.LoadAssetAtPath<EchoArtTheme>(Root+"/Resources/EchoArtTheme.asset");
            if(theme==null){theme=ScriptableObject.CreateInstance<EchoArtTheme>();AssetDatabase.CreateAsset(theme,Root+"/Resources/EchoArtTheme.asset");}
            theme.panel=Panel("ui-panel",Ivory,Ink);theme.button=Panel("ui-button",Ivory,new Color(.08f,.08f,.12f));theme.buttonHover=Panel("ui-button-hover",Teal,new Color(.09f,.19f,.19f));theme.buttonPressed=Panel("ui-button-pressed",Coral,new Color(.22f,.10f,.13f));theme.buttonDisabled=Panel("ui-button-disabled",Color.gray,new Color(.08f,.08f,.10f));
            theme.cardFrame=Texture("ui-card-frame",350,500,(x,y)=>{
                int d=Math.Min(Math.Min(x,y),Math.Min(349-x,499-y));if(x+y<16||x+y>832)return Color.clear;if(d<3)return Ink;if(d<8)return Ivory;if(d<12)return Ink;
                if(x>16&&x<333&&y>151&&y<407)return Color.clear;
                if((y==146||y==412)&&x>22&&x<328)return Ivory;
                if(x<22&&y>154&&y<404)return Teal;
                if(x>300&&y>465&&x+y>790)return Coral;
                return Ink;
            });
            theme.cardOutline=Texture("ui-card-outline",350,500,(x,y)=>{
                int d=Math.Min(Math.Min(x,y),Math.Min(349-x,499-y));
                return d>=2&&d<=5?Color.white:Color.clear;
            });
            theme.attackIcon=Icon("icon-attack",0);theme.blockIcon=Icon("icon-block",1);theme.noiseIcon=Icon("icon-noise",2);theme.memoryIcon=Icon("icon-memory",3);
            EditorUtility.SetDirty(theme);AssetDatabase.SaveAssets();Debug.Log("IVORY_UI_CREATED");
        }
        static Sprite Icon(string name,int kind)
        {
            var result=Texture(name,64,64,(x,y)=>{
                bool on=kind==0?(Math.Abs(x-y)<4&&x>12&&x<53)||(Math.Abs(x+y-44)<3&&x>10&&x<35):kind==1?((x>16&&x<48&&y>28&&y<53)||(y>10&&y<=28&&Math.Abs(x-32)<(y-7))):kind==2?(Math.Abs(y-32)<3||Math.Abs(x-32)<3&&y>12&&y<52||Math.Abs(x-20)<2&&y>24&&y<40||Math.Abs(x-44)<2&&y>21&&y<43):((x>9&&x<55&&y>16&&y<48)&&!(x>13&&x<51&&y>20&&y<44))||((x-23)*(x-23)+(y-32)*(y-32)<20)||((x-42)*(x-42)+(y-32)*(y-32)<20);
                return on?(kind==0?Coral:kind==2?Teal:Ivory):Color.clear;
            });
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/"+name+".png");importer.spritePixelsPerUnit=100;importer.SaveAndReimport();return result;
        }
        [MenuItem("Variables Echo/Art/Install Ivory and Void")]
        public static void Install()
        {
            BuildUI();var t=Load<EchoArtTheme>(Root+"/Resources/EchoArtTheme.asset");
            t.warden=Image("hero-warden",true);t.salvager=Image("hero-salvager",true);t.ivoryBackground=Image("background-ivory");t.wastelandBackground=Image("background-waste");t.neonBackground=Image("background-neon");
            var data=Load<EchoChapterData>(ChapterOneBuilder.DataPath);
            string heroPath=Root+"/Warden.prefab";
            CopyCharacter(data.hero.Prefab,heroPath,t.warden,false);
            data.hero.Prefab=Load<GameObject>(heroPath);EditorUtility.SetDirty(data.hero);
            var alt=data.alternateHero;
            if(alt==null){alt=UnityEngine.Object.Instantiate(data.hero);AssetDatabase.CreateAsset(alt,Root+"/Salvager.asset");data.alternateHero=alt;}
            CopyCharacter(data.hero.Prefab,Root+"/Salvager.prefab",t.salvager,false);alt.Name="Mneme";alt.Prefab=Load<GameObject>(Root+"/Salvager.prefab");EditorUtility.SetDirty(alt);
            string[] names={"enemy-observer","enemy-listener","enemy-sentinel","enemy-keeper","enemy-emissary"};
            string[] nodes={"gate","listener","sentinel","elite","boss"};
            for(int i=0;i<nodes.Length;i++)
            {
                var enemy=data.nodes.First(n=>n.id==nodes[i]).enemy;var art=Image(names[i],true);
                string path=Root+"/"+names[i]+".prefab";CopyCharacter(enemy.Prefab,path,art,true);enemy.Prefab=Load<GameObject>(path);
                foreach(var p in enemy.Patterns.OfType<RepeatPattern>())p.Sprite=p.Effects.Any(e=>e is DealDamageEffect)?t.attackIcon:t.blockIcon;
                if(enemy.EchoProfile!=null){foreach(var c in enemy.EchoProfile.counters)c.icon=t.noiseIcon;EditorUtility.SetDirty(enemy.EchoProfile);}
                foreach(var p in enemy.Patterns)EditorUtility.SetDirty(p);
                EditorUtility.SetDirty(enemy);
            }
            var memory=Image("card-memory");var refusal=Image("card-refusal");var identity=Image("card-identity");
            foreach(var c in data.cards){int id=c.Id>=1200?c.Id-1000:c.Id;c.Picture=new[]{201,203,207,208,212}.Contains(id)?refusal:new[]{202,206,210}.Contains(id)?identity:memory;EditorUtility.SetDirty(c);}
            foreach(var n in data.nodes)
            {
                n.region=new[]{"market","survivor"}.Contains(n.id)?EchoArtRegion.Wasteland:new[]{"listener","elite"}.Contains(n.id)?EchoArtRegion.Interference:EchoArtRegion.Ivory;
                n.background=n.region==EchoArtRegion.Wasteland?t.wastelandBackground:n.region==EchoArtRegion.Interference?t.neonBackground:t.ivoryBackground;
            }
            EditorUtility.SetDirty(t);EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
            var scene=EditorSceneManager.OpenScene(ChapterOneBuilder.Root+"/ChapterBattle.unity");
            if(UnityEngine.Object.FindAnyObjectByType<EchoBattleVisualSkin>()==null)new GameObject("Ivory Battle Skin").AddComponent<EchoBattleVisualSkin>();
            var bootstrap=UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();
            if(GameObject.Find("ArtFloorAligned")==null)
            {
                bootstrap.playerPivot.position+=Vector3.down*.7f;
                foreach(var p in bootstrap.enemyPivots1) p.position+=Vector3.down*.7f;
                foreach(var p in bootstrap.enemyPivots2) p.position+=Vector3.down*.7f;
                new GameObject("ArtFloorAligned");
            }
            foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))if(r.sortingLayerName=="Background")r.sprite=t.ivoryBackground;
            EditorSceneManager.SaveScene(scene);
            // Retheme the existing demo entry as well, while retaining its original encounter statistics.
            var demoHero=AssetDatabase.LoadAssetAtPath<HeroTemplate>("Assets/VariablesEcho/Demo/Data/Heroes/Warden.asset");
            demoHero.Prefab=data.hero.Prefab;EditorUtility.SetDirty(demoHero);
            var demoEnemy=AssetDatabase.LoadAssetAtPath<EnemyTemplate>("Assets/VariablesEcho/Demo/Data/Enemies/NexusMarauder.asset");
            demoEnemy.Prefab=data.nodes.First(n=>n.id=="sentinel").enemy.Prefab;EditorUtility.SetDirty(demoEnemy);
            foreach(var guid in AssetDatabase.FindAssets("t:CardTemplate",new[]{"Assets/VariablesEcho/Demo/Data"}))
            {
                var c=AssetDatabase.LoadAssetAtPath<CardTemplate>(AssetDatabase.GUIDToAssetPath(guid));c.Picture=c.Name.Contains("Defend")?identity:c.Name.Contains("Noise")?memory:refusal;EditorUtility.SetDirty(c);
            }
            foreach(var statusName in new[]{"Weak","Strength"})
            {
                var status=AssetDatabase.LoadAssetAtPath<StatusTemplate>("Assets/SinglePlayerCCGKit/GameData/Status/"+statusName+".asset");
                if(status!=null){status.Sprite=statusName=="Weak"?t.noiseIcon:t.attackIcon;EditorUtility.SetDirty(status);}
            }
            AssetDatabase.SaveAssets();
            var demoScene=EditorSceneManager.OpenScene("Assets/VariablesEcho/Demo/Scenes/BattleDemo.unity");
            foreach(var info in UnityEngine.Object.FindObjectsByType<GameInfo>(FindObjectsSortMode.None))info.Encounter.Background=t.neonBackground;
            if(UnityEngine.Object.FindAnyObjectByType<EchoBattleVisualSkin>()==null)new GameObject("Ivory Battle Skin").AddComponent<EchoBattleVisualSkin>();
            EditorSceneManager.SaveScene(demoScene);
            EditorSceneManager.OpenScene(ChapterOneBuilder.Root+"/ChapterOne.unity");
            ChapterOneBuilder.Validate();Debug.Log("IVORY_ART_INSTALL_PASS");
            ChapterBuildPlayer.Build();
        }
        static void CopyCharacter(GameObject source,string path,Sprite art,bool enemy)
        {
            // Independent prefab copies preserve the Kit collider/events while replacing all visual children.
            var go=UnityEngine.Object.Instantiate(source);go.name=Path.GetFileNameWithoutExtension(path);
            foreach(var flash in go.GetComponentsInChildren<EchoSpriteHitFlash>())UnityEngine.Object.DestroyImmediate(flash);
            foreach(var renderer in go.GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.sprite=art;renderer.color=Color.white;
                var size=art.bounds.size;float h=enemy?6.0f:6.4f;float scale=h/size.y;
                renderer.transform.localScale=Vector3.one*scale;renderer.transform.localPosition=Vector3.zero;renderer.flipX=false;
                renderer.sortingLayerName="Characters";
                var collider=go.GetComponent<BoxCollider2D>();collider.size=new Vector2(size.x*scale,h);collider.offset=new Vector2(0,h*.5f);
            }
            PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);
        }
    }
}



