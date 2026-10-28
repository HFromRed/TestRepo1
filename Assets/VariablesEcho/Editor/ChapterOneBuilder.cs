using System;
using System.Collections.Generic;
using System.Linq;
using CCGKit;
using VariablesEcho;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VariablesEchoEditor
{
    public static class ChapterOneBuilder
    {
        public const string Root="Assets/VariablesEcho/ChapterOne";
        public const string DataPath=Root+"/ChapterOne.asset";
        static EchoChapterData data;
        static string Demo="Assets/VariablesEcho/Demo";
        static T Load<T>(string p) where T:UnityEngine.Object {var a=AssetDatabase.LoadAssetAtPath<T>(p);if(a==null)throw new Exception("Missing asset: "+p);return a;}
        static T Asset<T>(T a,string name) where T:UnityEngine.Object {AssetDatabase.CreateAsset(a,Root+"/"+name+".asset");return a;}
        [MenuItem("Variables Echo/Chapter One/Create Chapter (Preserve Existing)")]
        public static void Build()
        {
            data=AssetDatabase.LoadAssetAtPath<EchoChapterData>(DataPath);
            if(data!=null) { Validate(); Debug.Log("Chapter exists; preserving designer edits."); return; }
            data=Asset(ScriptableObject.CreateInstance<EchoChapterData>(),"ChapterOne");
            var strike=Card(201,"Strike",1,6,0,0,0);
            var defend=Card(202,"Defend",1,0,5,0,0);
            var bash=Card(203,"Bash",2,8,0,0,0); Status(bash,2);
            var charge=Card(204,"Noise Charge",0,0,0,0,25); Effect<GainManaEffect>(charge,2,EffectTargetType.Self);
            var scan=Card(205,"Read Between",1,0,3,2,-8);
            var silence=Card(206,"Quiet Room",1,0,8,0,-12);
            var pulse=Card(207,"Feedback",1,12,0,0,12);
            var refusal=Card(208,"Refusal",2,14,6,0,8);
            var question=Card(209,"Open Question",0,0,0,1,8);
            var mirror=Card(210,"Mirror Guard",1,0,6,0,0); Effect<ReflectEffect>(mirror,3,EffectTargetType.Self);
            var doubt=Card(211,"Doubt",1,4,0,1,-6); Status(doubt,1);
            var proof=Card(212,"Proof of Life",2,18,0,0,0); Effect<SelfDamageEffect>(proof,3,EffectTargetType.Self); proof.IsSelfHarm=true; proof.Effects.Insert(0,proof.Effects[proof.Effects.Count-1]);proof.Effects.RemoveAt(proof.Effects.Count-1);
            foreach(var c in data.cards.ToArray()) Upgrade(c);
            data.rewards.AddRange(new[]{scan,silence,pulse,refusal,question,mirror,doubt,proof,charge});
            var lib=Asset(ScriptableObject.CreateInstance<CardLibrary>(),"StartingDeck");
            lib.Name="Grey-7";
            lib.Entries.Add(new CardLibraryEntry{Card=strike,NumCopies=5});lib.Entries.Add(new CardLibraryEntry{Card=defend,NumCopies=4});lib.Entries.Add(new CardLibraryEntry{Card=bash,NumCopies=1});
            var rewards=Asset(ScriptableObject.CreateInstance<CardLibrary>(),"RewardLibrary");
            foreach(var c in data.cards) rewards.Entries.Add(new CardLibraryEntry{Card=c,NumCopies=1});
            var hero=UnityEngine.Object.Instantiate(Load<HeroTemplate>(Demo+"/Data/Heroes/Warden.asset"));
            EditorUtility.SetDirty(lib); EditorUtility.SetDirty(rewards);
            hero.Name="Grey-7";hero.StartingDeck=lib;hero.RewardDeck=rewards;data.hero=Asset(hero,"Grey7");
            var drone=Enemy("Gate Observer",32,new[]{6,4,8},new[]{0,4,0},new[]{0,0,0},false,false);
            var listener=Enemy("Signal Listener",44,new[]{7,0,10},new[]{0,7,0},new[]{0,12,0},false,false);
            var sentinel=Enemy("Archive Sentinel",56,new[]{9,5,12},new[]{0,6,0},new[]{0,8,0},false,false);
            var elite=Enemy("Model Keeper",78,new[]{11,6,15},new[]{0,8,0},new[]{0,12,0},true,false);
            var boss=Enemy("Grey-7 Emissary",112,new[]{12,5,17,7},new[]{0,10,0,5},new[]{0,10,0,0},true,true);
            Node("gate",0,"01  入站检查",ChapterNodeKind.Battle,"系统：敌人意图已为你标注。攻击发声，防御校准。先学会活着，再问为什么。",drone);
            var e=Node("first-noise",1,"02  不合规的念头",ChapterNodeKind.Event,"通道里有人哼唱一段没有用途的旋律。你的校准环要求你停止倾听。");
            e.choices.Add(new ChapterChoice{label="记住旋律 / 获得 Noise Charge，下一战初始噪声 +15，记忆 +1",card=charge,noise=15,memory=1,consequence="你记住了旋律。它没有用途，但它属于你。"});
            e.choices.Add(new ChapterChoice{label="记录频率 / 获得 Read Between，信用 +10",card=scan,credits=10,consequence="你把旋律变成数据。数据保住了形状，是否也保住了意义？"});
            Node("listener",2,"03  信号井",ChapterNodeKind.Battle,"干扰会抬高噪声。用护盾重新看清意图，或用失真换取爆发。",listener);
            e=Node("survivor",3,"04  被删去的人",ChapterNodeKind.Event,"一个维修工请求你留下他的名字。系统说备份中没有这个人：没有记录，是否等于没有存在过？");
            e.choices.Add(new ChapterChoice{label="保存证词 / 失去 6 生命，获得 Doubt，记忆 +1",heal=-6,card=doubt,memory=1,consequence="你用自己的存储区留下他的名字，校准环刺入神经。证据现在有了代价。"});
            e.choices.Add(new ChapterChoice{label="交换补给 / 回复 12 生命，信用 +15",heal=12,credits=15,consequence="他给了你修复剂。你没有留下名字，但你们确实帮助过彼此。"});
            Node("market",3,"04  废弃交换机",ChapterNodeKind.Shop,"没有免费选择。这里可以买牌，也可以买回牌组里的一点空白。");
            Node("sentinel",4,"05  档案侧门",ChapterNodeKind.Battle,"较安全的路线。守卫只会重复指令，它不知道自己守着谁的过去。",sentinel);
            var en=Node("elite",4,"05  模型保管者",ChapterNodeKind.Elite,"危险路线。它会识别最近三回合的习惯。胜利额外获得一份记忆和更多信用。",elite);en.credits=45;
            Node("rest",5,"06  无人维修舱",ChapterNodeKind.Rest,"修复身体，或改写一条习惯。设备的电量只够完成一件事。");
            e=Node("mirror-log",6,"07  双重档案",ChapterNodeKind.Event,"你发现两份同名档案。一个灰-7接受过校准，另一个拒绝过。两份文件的结束时间都是今天。");
            e.choices.Add(new ChapterChoice{label="复制异常 / 获得 Refusal，下一战初始噪声 +25，记忆 +1",card=refusal,noise=25,memory=1,consequence="你决定承担不确定。新牌增强了你，也挤占了你抽到旧牌的机会。"});
            e.choices.Add(new ChapterChoice{label="请求校验 / 获得 Quiet Room，回复 8 生命",card=silence,heal=8,consequence="系统发来一份完美的解释。你恢复了体力，却无法独立验证它。"});
            Node("last-rest",7,"08  静默前室",ChapterNodeKind.Rest,"门后传来自己的呼吸声。最后一次维修机会。准备好了再进去。");
            Node("boss",8,"09  镜像使者",ChapterNodeKind.Boss,"对方也是人类战士。它学会了你的攻击与防守，却仍旧执行着不属于它的任务。",boss);
            EditorUtility.SetDirty(data); AssetDatabase.SaveAssets();
            BuildScenes(); Validate();
            Debug.Log("CHAPTER_BUILD_OK: "+data.cards.Count+" cards; "+data.nodes.Count+" nodes.");
        }
        static CardTemplate Card(int id,string name,int cost,int damage,int shield,int draw,int noise)
        {
            var c=ScriptableObject.CreateInstance<CardTemplate>();c.Id=id;c.CardId="ch1-"+id;c.Name=name;c.Cost=cost;c.NoiseDelta=noise;
            c.Faction=noise>0?EchoCardFaction.Noise:EchoCardFaction.Logic;
            var source=Load<CardTemplate>(Demo+"/Data/Cards/"+(damage>0?"Strike":"Defend")+".asset");
            c.Picture=source.Picture;c.Material=source.Material;c.Type=source.Type;
            Asset(c,"Card_"+id);data.cards.Add(c);
            if(damage>0)Effect<DealDamageEffect>(c,damage,EffectTargetType.TargetEnemy);
            if(shield>0)Effect<GainShieldEffect>(c,shield,EffectTargetType.Self);
            if(draw>0){var d=ScriptableObject.CreateInstance<DrawCardsEffect>();d.Amount=draw;AssetDatabase.AddObjectToAsset(d,c);c.Effects.Add(d);}
            // Include the cost in the visible effect text once; NoiseDelta is the sole owner of the mutation.
            if(noise!=0){var d=ScriptableObject.CreateInstance<ChapterNoiseDescription>();d.amount=noise;AssetDatabase.AddObjectToAsset(d,c);c.Effects.Add(d);}
            EditorUtility.SetDirty(c);return c;
        }
        static T Effect<T>(CardTemplate c,int value,EffectTargetType target) where T:IntegerEffect
        {var e=ScriptableObject.CreateInstance<T>();e.Value=value;e.Target=target;AssetDatabase.AddObjectToAsset(e,c);c.Effects.Add(e);EditorUtility.SetDirty(c);return e;}
        static void Status(CardTemplate c,int value) {var e=Effect<ApplyStatusEffect>(c,value,EffectTargetType.TargetEnemy);e.Status=Load<StatusTemplate>("Assets/SinglePlayerCCGKit/GameData/Status/Weak.asset");}
        static void Upgrade(CardTemplate c)
        {
            var u=UnityEngine.Object.Instantiate(c);u.Id=c.Id+1000;u.CardId=c.CardId+"-plus";u.Name=c.Name+" +";u.Effects=new List<Effect>();
            Asset(u,"Card_"+u.Id);
            foreach(var old in c.Effects){var e=UnityEngine.Object.Instantiate(old);if(e is DealDamageEffect d)d.Value+=3;else if(e is GainShieldEffect s)s.Value+=3;AssetDatabase.AddObjectToAsset(e,u);u.Effects.Add(e);}
            if(!u.Effects.Any(e=>e is DealDamageEffect || e is GainShieldEffect)){ if(u.Cost>0)u.Cost--;else {u.NoiseDelta=Mathf.Max(0,u.NoiseDelta-8);foreach(var d in u.Effects.OfType<ChapterNoiseDescription>())d.amount=u.NoiseDelta;} }
            c.Upgrade=u;data.cards.Add(u);EditorUtility.SetDirty(c);EditorUtility.SetDirty(u);
        }
        static EnemyTemplate Enemy(string name,int hp,int[] damage,int[] shield,int[] noise,bool counter,bool human)
        {
            var e=ScriptableObject.CreateInstance<EnemyTemplate>();e.Name=name;e.HpLow=e.HpHigh=hp;
            e.Prefab=Load<GameObject>(Demo+"/Prefabs/NexusMarauder.prefab");Asset(e,"Enemy_"+name.Replace(" ",""));
            var visual=PrefabUtility.InstantiatePrefab(e.Prefab) as GameObject;
            foreach(var f in visual.GetComponentsInChildren<EchoSpriteHitFlash>())UnityEngine.Object.DestroyImmediate(f);
            var renderer=visual.GetComponentInChildren<SpriteRenderer>();
            if(human) {renderer.sprite=Load<GameObject>(Demo+"/Prefabs/Warden.prefab").GetComponentInChildren<SpriteRenderer>().sprite;renderer.transform.localScale=Vector3.one;}
            renderer.color=human?new Color(.7f,.9f,1):Color.Lerp(new Color(.35f,.9f,.85f),new Color(1,.45f,.6f),hp/115f);
            e.Prefab=PrefabUtility.SaveAsPrefabAsset(visual,Root+"/Enemy_"+name.Replace(" ","")+".prefab");UnityEngine.Object.DestroyImmediate(visual);
            for(int i=0;i<damage.Length;i++)
            {
                var p=ScriptableObject.CreateInstance<RepeatPattern>();p.name="Sequence "+(i+1);p.Times=1;
                p.Sprite=Load<Sprite>(Demo+"/Art/UI/IntentIcons/"+(damage[i]>0?"attack":"block")+".png");AssetDatabase.AddObjectToAsset(p,e);
                if(damage[i]>0)p.Effects.Add(EnemyEffect<DealDamageEffect>(e,damage[i],EffectTargetType.TargetEnemy));
                if(shield[i]>0)p.Effects.Add(EnemyEffect<GainShieldEffect>(e,shield[i],EffectTargetType.Self));
                if(noise[i]>0)p.Effects.Add(EnemyEffect<NoiseEffect>(e,noise[i],EffectTargetType.Self));
                e.Patterns.Add(p);
            }
            if(counter)
            {
                var profile=ScriptableObject.CreateInstance<EchoEnemyProfile>();profile.faction=EchoAiFaction.NEXUS;AssetDatabase.AddObjectToAsset(profile,e);
                profile.counters.Add(new EchoCounterEntry{tag=EchoBehaviorTag.AllIn,label="I know your anger",icon=((RepeatPattern)e.Patterns[0]).Sprite,effects=new List<Effect>{EnemyEffect<GainShieldEffect>(e,8,EffectTargetType.Self),EnemyEffect<DealDamageEffect>(e,7,EffectTargetType.TargetEnemy)}});
                profile.counters.Add(new EchoCounterEntry{tag=EchoBehaviorTag.Wall,label="I know your fear",icon=((RepeatPattern)e.Patterns[0]).Sprite,effects=new List<Effect>{EnemyEffect<DealDamageEffect>(e,10,EffectTargetType.TargetEnemy),EnemyEffect<NoiseEffect>(e,15,EffectTargetType.Self)}});
                e.EchoProfile=profile;
            }
            EditorUtility.SetDirty(e);return e;
        }
        static T EnemyEffect<T>(EnemyTemplate e,int v,EffectTargetType t)where T:IntegerEffect{var x=ScriptableObject.CreateInstance<T>();x.Value=v;x.Target=t;AssetDatabase.AddObjectToAsset(x,e);return x;}
        static ChapterNode Node(string id,int depth,string title,ChapterNodeKind kind,string story,EnemyTemplate enemy=null)
        {var n=new ChapterNode{id=id,depth=depth,title=title,kind=kind,story=story,enemy=enemy,background=Load<Sprite>(Demo+"/Art/Backgrounds/nexus_arena.png")};data.nodes.Add(n);return n;}
        static void BuildScenes()
        {
            string battle=Root+"/ChapterBattle.unity",map=Root+"/ChapterOne.unity";
            AssetDatabase.CopyAsset(Demo+"/Scenes/BattleDemo.unity",battle);
            var scene=EditorSceneManager.OpenScene(battle);
            foreach(var g in UnityEngine.Object.FindObjectsByType<GameInfo>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(g.gameObject);
            new GameObject("Chapter Stage").AddComponent<EchoBattleStageFix>();
            EditorSceneManager.SaveScene(scene);
            var menu=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.04f,.06f);camera.gameObject.AddComponent<AudioListener>();
            new GameObject("Chapter Director").AddComponent<EchoChapterController>().Data=data;
            EditorSceneManager.SaveScene(menu,map);
            var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=map&&s.path!=battle).ToList();scenes.Insert(0,new EditorBuildSettingsScene(battle,true));scenes.Insert(0,new EditorBuildSettingsScene(map,true));EditorBuildSettings.scenes=scenes.ToArray();
        }
        public static void FinalizeContent()
        {
            data=Load<EchoChapterData>(DataPath);
            foreach(var card in data.cards)
            {
                var cost=card.Effects.OfType<SelfDamageEffect>().FirstOrDefault();
                if(cost!=null){card.Effects.Remove(cost);card.Effects.Insert(0,cost);EditorUtility.SetDirty(card);}
            }
            AssetDatabase.SaveAssets();
            EchoDistortionSelfTest.Run();
            ChapterBuildPlayer.Build();
        }
        public static void RepairInitialLibraries()
        {
            data=Load<EchoChapterData>(DataPath);
            var lib=data.hero.StartingDeck;
            if(lib.Entries.Count==0)
            {
                lib.Name="Grey-7";
                lib.Entries.Add(new CardLibraryEntry{Card=data.FindCard(201),NumCopies=5});
                lib.Entries.Add(new CardLibraryEntry{Card=data.FindCard(202),NumCopies=4});
                lib.Entries.Add(new CardLibraryEntry{Card=data.FindCard(203),NumCopies=1});
                EditorUtility.SetDirty(lib);
            }
            var rewards=data.hero.RewardDeck;
            if(rewards.Entries.Count==0){foreach(var c in data.cards)rewards.Entries.Add(new CardLibraryEntry{Card=c,NumCopies=1});EditorUtility.SetDirty(rewards);}
            AssetDatabase.SaveAssets();
            ChapterBuildPlayer.Build();
        }
        [MenuItem("Variables Echo/Chapter One/Validate Chapter")]
        public static void Validate()
        {
            data=Load<EchoChapterData>(DataPath);
            void Check(bool ok,string message){if(!ok)throw new Exception("Chapter validation: "+message);}
            Check(data.hero.StartingDeck.Entries.Sum(e=>e.NumCopies)>=5,"starting deck must contain at least five cards");
            Check(data.cards.Select(c=>c.Id).Distinct().Count()==data.cards.Count,"duplicate card IDs");
            Check(data.cards.Select(c=>c.CardId).Distinct().Count()==data.cards.Count,"duplicate stable IDs");
            Check(data.nodes.Select(n=>n.id).Distinct().Count()==data.nodes.Count,"duplicate node IDs");
            for(int i=0;i<=data.maxDepth;i++)Check(data.nodes.Any(n=>n.depth==i),"unreachable depth "+i);
            Check(data.nodes.Where(n=>n.depth==data.maxDepth).All(n=>n.kind==ChapterNodeKind.Boss),"last depth must end chapter");
            foreach(var c in data.cards){Check(c.Material!=null && c.Picture!=null,c.Name+" missing art");Check(!(c.NoiseDelta!=0&&c.Effects.Any(e=>e is NoiseEffect)),c.Name+" double noise");}
            foreach(var n in data.nodes){if(n.kind==ChapterNodeKind.Battle||n.kind==ChapterNodeKind.Elite||n.kind==ChapterNodeKind.Boss)Check(n.enemy!=null && n.enemy.Patterns.Count>0,n.id+" missing enemy");}
            Debug.Log("CHAPTER_VALIDATION_OK");
        }
    }
}



