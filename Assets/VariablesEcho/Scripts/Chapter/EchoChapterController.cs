using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CCGKit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VariablesEcho
{
    // Chapter orchestration owns progression; combat remains entirely in CCG Kit + EchoRuntime.
    public class EchoChapterController : MonoBehaviour
    {
        public static EchoChapterController Active { get; private set; }
        public EchoChapterData Data;
        EchoChapterData campaignSource;
        public string ChapterTitle { get { return State==null ? EchoCampaign.Title(1) : EchoCampaign.Title(State.act); } }
        void LoadAct(int act)
        {
            if(campaignSource==null)campaignSource=Data;
            var previous=Data;
            Data=EchoCampaign.Create(campaignSource,act);
            StrengthRewardSet.Install(Data);
            if(previous!=campaignSource)Destroy(previous);
        }
        public ChapterSave State { get; private set; }
        public ChapterNode CurrentNode { get { return State == null ? null : Data.nodes.Find(n => n.id == State.nodeId); } }
        public const string MapScene = "ChapterOne", BattleScene = "ChapterBattle";
        public static string SavePath { get { return Path.Combine(EchoSaveService.StorageRoot, "chapter-one.json"); } }
        string screen = "Title", notice = "", selection = "";
        bool busy, showDeck, help;
        int selectedHero;
        bool hasContinue;
        int clearCount;
        public HeroTemplate CurrentHero { get { return State != null && State.heroVariant == 1 && Data.alternateHero != null ? Data.alternateHero : Data.hero; } }
        Vector2 scroll;
        Font font;
        GUIStyle title, heading, body, button, small;
        void Awake()
        {
            if (Active != null && Active != this) { Destroy(gameObject); return; }
            Active = this;
            screen="Title";
            RefreshTitle();
            campaignSource=Data;
            LoadAct(1);
            DontDestroyOnLoad(gameObject);
            EchoRuntime.EnsureInitialized();
            EchoEvents.BattleStarted += OnBattleStarted;
            EchoEvents.BattleEnded += OnBattleEnded;
        }
        void OnDestroy()
        {
            if (Active != this) return;
            EchoEvents.BattleStarted -= OnBattleStarted;
            EchoEvents.BattleEnded -= OnBattleEnded;
            Active = null;
        }
        public List<CardTemplate> GetDeck() { return State.deck.Select(Data.FindCard).Where(c => c != null).ToList(); }
        public void NewRun(int seed = 0)
        {
            LoadAct(1);
            readingKey = "";
            busy=false;help=false;queuedMapVisit=null;
            State = new ChapterSave { runId = Guid.NewGuid().ToString("N"), seed = seed == 0 ? Environment.TickCount : seed, hp = (selectedHero==1 && Data.alternateHero!=null?Data.alternateHero:Data.hero).Hp, heroVariant = selectedHero };
            foreach (var e in CurrentHero.StartingDeck.Entries)
                for (int i = 0; i < e.NumCopies; i++) State.deck.Add(e.Card.Id);
            EchoRuntime.Meta.StartNewRun(EchoRuntime.Config, EchoAiFaction.NEXUS, State.heroVariant==1?EchoPlayerClass.Render:EchoPlayerClass.Warden, State.seed);
            State.journal.Add("任务：关闭镜像中继站。执行人员：云虎崽。");
            notice = ""; screen = State.phase = "Intro"; showDeck = false; scroll=Vector2.zero; Save();
        }
        public bool Resume()
        {
            try
            {
                var saved = JsonUtility.FromJson<ChapterSave>(File.ReadAllText(SavePath));
                if (saved != null && (saved.phase == "Defeat" || (saved.phase == "Complete" && !string.IsNullOrEmpty(saved.epilogue))))
                { RefreshTitle(); notice="旅程已结束，请开始新游戏。"; return false; }
                if(saved!=null && saved.act==0)saved.act=1;
                if(saved==null || saved.act<1 || saved.act>3)throw new InvalidDataException("章节编号无效");
                var loaded=EchoCampaign.Create(campaignSource,saved.act);
                StrengthRewardSet.Install(loaded);
                StrengthRewardSet.MigrateSave(saved);
                var previousData=Data; Data=loaded;
                if (saved == null || !new[] { "Map", "Node", "Battle", "Reward", "Ending", "Complete", "Defeat", "Intro", "Consequence", "ChapterComplete" }.Contains(saved.phase) || saved.version != 1 || saved.deck.Count == 0 || saved.deck.Any(id => Data.FindCard(id) == null) || saved.depth < 0 || saved.depth > Data.maxDepth + 1 || (saved.nodeId != null && saved.nodeId.Length > 0 && !Data.nodes.Any(n => n.id == saved.nodeId)))
                { Data=previousData;Destroy(loaded);throw new InvalidDataException("内容版本或卡牌 ID 不兼容"); }
                if(previousData!=campaignSource)Destroy(previousData);
                State = saved;
                busy=false;help=false;showDeck=false;notice="";scroll=Vector2.zero;
                // Older first-chapter completion checkpoints continue into the campaign.
                if(State.phase=="Complete" && string.IsNullOrEmpty(State.epilogue))
                {State.phase="ChapterComplete";State.keptMemory=State.ending=="留下裂缝";}
                if(State.journal==null)State.journal=new List<string>();
                if (State.visitedNodeIds == null) State.visitedNodeIds = new List<string>();
                // Legacy saves contain no route history; allow their current row once.
                if (State.depth > 0 && string.IsNullOrEmpty(State.completedNodeId)) State.completedNodeId = "legacy";
                if (State.phase == "Battle") { screen="Node"; EnterBattle(); return true; }
                screen = State.phase;
                readingKey = "";
                return true;
            }
            catch (Exception e) { notice = "无法继续存档：" + e.Message + "。可以开始新旅程。"; return false; }
        }
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(EchoSaveService.StorageRoot);
                var temp = SavePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(State, true));
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, SavePath + ".bak");
                else File.Move(temp, SavePath);
                EchoRuntime.Meta.SaveRun(); EchoRuntime.Meta.SaveMeta();
                hasContinue = State.phase != "Complete" && State.phase != "Defeat";
            }
            catch (Exception e) { notice = "存档失败，请勿退出：" + e.Message; Debug.LogError(notice); }
        }
        public bool CanVisit(ChapterNode node)
        {
            if (node == null || State == null || node.depth != State.depth) return false;
            if (State.completedNodeId == "legacy") return true;
            if (string.IsNullOrEmpty(State.completedNodeId)) return node.depth == 0;
            var previous = Data.nodes.Find(n => n.id == State.completedNodeId);
            return previous != null && previous.nextNodeIds != null && previous.nextNodeIds.Contains(node.id);
        }
        public void Visit(string id)
        {
            if (busy || screen != "Map") return;
            var node = Data.nodes.Find(n => n.id == id);
            if (!CanVisit(node)) return;
            State.nodeId = id; State.shopBought = State.shopRemoved = false;
            State.journal.Add(node.title + "：" + node.story);
            screen = State.phase = "Node"; Save();
        }
        public void EnterBattle()
        {
            if (busy || CurrentNode == null || CurrentNode.enemy == null) return;
            busy = true; State.phase = "Battle"; screen = "Battle"; showDeck = false; help = false; Save();
            EchoRuntime.Meta.ResetCombatRandom(State.seed ^ (State.depth * 7919) ^ (State.act*104729));
            UnityEngine.Random.InitState(State.seed ^ (State.depth * 7919) ^ (State.act*104729));
            // Restarting a battle is an explicit checkpoint, not a mid-turn save.
            foreach (var info in FindObjectsByType<GameInfo>(FindObjectsSortMode.None)) Destroy(info.gameObject);
            SceneManager.LoadScene(BattleScene);
        }
        void OnBattleStarted(EchoBattleContext battle)
        {
            if (screen != "Battle") return;
            busy = false;
            battle.Noise.Add(State.startingNoise, "chapter-event");
        }
        void OnBattleEnded(bool died)
        {
            if (screen != "Battle" || busy) return;
            busy = true;
            State.hp = EchoRuntime.Battle != null ? Mathf.Max(0, EchoRuntime.Battle.PlayerRuntime.Hp.Value) : 0;
            State.startingNoise = 0;
            if (died) { screen = State.phase = "Defeat"; }
            else if (CurrentNode.kind == ChapterNodeKind.Boss) { screen = State.phase = "Ending"; }
            else
            {
                State.credits += CurrentNode.credits;
                if (CurrentNode.kind == ChapterNodeKind.Elite) { State.memory++; State.journal.Add("获得镜像校验片，已存入记忆记录。"); }
                var rng = new System.Random(State.seed ^ (State.depth * 3571));
                State.reward = Data.rewards.OrderBy(c => c.Id).OrderBy(c => rng.Next()).Take(3).Select(c => c.Id).ToList();
                screen = State.phase = "Reward";
            }
            Save(); StartCoroutine(ReturnToMap());
        }
        IEnumerator ReturnToMap()
        {
            yield return null;
            DG.Tweening.DOTween.KillAll(); // Finish scene-owned animations before their targets are destroyed.
            SceneManager.LoadScene(MapScene);
            busy = false;
        }
        public void ChooseReward(int id)
        {
            if (screen != "Reward" || (id != -1 && !State.reward.Contains(id))) return;
            if (id != -1) State.deck.Add(id);
            State.reward.Clear(); Advance();
        }
        public void ChooseEvent(int index)
        {
            if (screen != "Node" || CurrentNode.kind != ChapterNodeKind.Event || index < 0 || index >= CurrentNode.choices.Count) return;
            var c = CurrentNode.choices[index];
            if (State.credits + c.credits < 0 || State.hp + c.heal <= 0) return;
            State.hp = Mathf.Min(CurrentHero.Hp, State.hp + c.heal); State.credits += c.credits;
            State.memory += c.memory; State.startingNoise = Mathf.Clamp(State.startingNoise + c.noise, 0, 100);
            if (c.card != null) State.deck.Add(c.card.Id);
            State.journal.Add(c.label + "：" + c.consequence);
            State.resultTitle=c.label;
            State.resultText=string.IsNullOrEmpty(c.consequence)?"你作出了选择，旅程将继续。":c.consequence;
            State.resultEffects=$"生命 {c.heal:+0;-0;0} · 数据 {c.credits:+0;-0;0} · 记忆 {c.memory:+0;-0;0} · 下战噪声 {c.noise:+0;-0;0}"+(c.card!=null?"\n获得卡牌："+c.card.Name:"");
            notice="";screen=State.phase="Consequence";scroll=Vector2.zero;Save();
        }
        public bool BuyCard()
        {
            if (screen != "Node" || CurrentNode.kind != ChapterNodeKind.Shop || State.shopBought || State.credits < Data.cardPrice) return false;
            var c = Data.rewards[Math.Abs(State.seed % Data.rewards.Count)];
            State.credits -= Data.cardPrice; State.deck.Add(c.Id); State.shopBought = true; Save(); return true;
        }
        public bool RemoveCard(int id)
        {
            if (screen != "Node" || CurrentNode.kind != ChapterNodeKind.Shop || State.shopRemoved || State.credits < Data.removePrice || State.deck.Count <= 5) return false;
            var c = Data.FindCard(id);
            if (c == null || c.IsPermanent || !State.deck.Contains(id)) return false;
            State.deck.Remove(id); State.credits -= Data.removePrice; State.shopRemoved = true; selection = ""; Save(); return true;
        }
        public void LeaveShop()
        {
            if (screen == "Node" && CurrentNode.kind == ChapterNodeKind.Shop) Advance();
        }
        public void Rest(int cardId = -1)
        {
            if (screen != "Node" || CurrentNode.kind != ChapterNodeKind.Rest) return;
            if (cardId == -1) State.hp = Mathf.Min(CurrentHero.Hp, State.hp + Data.restHeal);
            else
            {
                var c = Data.FindCard(cardId); int index = State.deck.IndexOf(cardId);
                if (c == null || c.Upgrade == null || index < 0) return;
                State.deck[index] = c.Upgrade.Id;
            }
            Advance();
        }
        void Advance()
        {
            State.completedNodeId = State.nodeId;
            if (!State.visitedNodeIds.Contains(State.nodeId)) State.visitedNodeIds.Add(State.nodeId);
            State.depth++; screen = State.phase = "Map"; selection = ""; scroll = Vector2.zero; Save();
        }
        public void ContinueConsequence()
        {
            if(screen!="Consequence")return;
            State.resultTitle=State.resultText=State.resultEffects="";
            Advance();
        }
        public void ContinueIntro()
        {
            if(screen!="Intro")return;
            State.storyPage++;
            if(State.storyPage>=IntroText().Length)
            { State.storyPage=0;screen=State.phase="Map"; }
            scroll=Vector2.zero;Save();
        }
        public void BeginNextChapter()
        {
            if(screen!="ChapterComplete" || State.act>=3)return;
            State.act++;LoadAct(State.act);
            State.depth=0;State.nodeId="";State.completedNodeId="";State.visitedNodeIds.Clear();
            State.reward.Clear();State.shopBought=State.shopRemoved=false;State.startingNoise=0;
            State.hp=CurrentHero.Hp;State.storyPage=0;State.ending="";
            EchoRuntime.SetAct(State.act);
            State.journal.Add(ChapterTitle+"：抵达新区域，补给恢复生命。牌组、数据与记忆保留。");
            screen=State.phase="Intro";notice=selection="";scroll=Vector2.zero;Save();
        }
        public void ResolveEnding(bool remember)
        {
            if(screen!="Ending")return;
            if(State.act==3){ResolveFinal(remember?1:0);return;}
            State.ending=State.act==1?"继续调查":"查明记录";
            State.journal.Add(State.act==1?"你前往档案馆，查找自己的任务记录。":"你带上记录，前往预测核心，查明自己的后续安排。");
            State.completedNodeId=State.nodeId;
            if(!State.visitedNodeIds.Contains(State.nodeId))State.visitedNodeIds.Add(State.nodeId);
            screen=State.phase="ChapterComplete";scroll=Vector2.zero;Save();
        }
        public void ResolveFinal(int choice)
        {
            if(screen!="Ending" || State.act!=3 || choice!=0)return;
            EchoAudioFeedback.Redeploy();
            State.ending="重新部署";
            State.epilogue = FinalStory(choice);
            State.completedNodeId=State.nodeId;
            if(!State.visitedNodeIds.Contains(State.nodeId))State.visitedNodeIds.Add(State.nodeId);
            State.journal.Add(State.ending+"："+State.epilogue);
            EchoRuntime.FinishRun(true,EchoEndingId.Calibration);
            screen=State.phase="Complete";scroll=Vector2.zero;Save();
            RecordCompletion();
        }
        void RecordCompletion()
        {
            if(State==null || State.phase!="Complete" || string.IsNullOrEmpty(State.epilogue))return;
            try { clearCount=EchoCompletionHistory.Record(State); }
            catch(Exception e) { notice="通关记录保存失败："+e.Message; Debug.LogError(notice); }
        }
        void RefreshTitle()
        {
            hasContinue=false;
            try
            {
                clearCount=EchoCompletionHistory.Count;
                if(!File.Exists(SavePath))return;
                var checkpoint=JsonUtility.FromJson<ChapterSave>(File.ReadAllText(SavePath));
                if(checkpoint==null)return;
                if(checkpoint.phase=="Complete" && !string.IsNullOrEmpty(checkpoint.epilogue))
                    clearCount=EchoCompletionHistory.Record(checkpoint);
                else hasContinue=checkpoint.phase!="Defeat";
            }
            catch(Exception e) { notice="存档读取失败："+e.Message; }
        }
        public void ReturnToTitle()
        {
            if(State!=null) { Save(); RecordCompletion(); }
            screen="Title";showDeck=help=false;busy=false;queuedMapVisit=null;
            selection="";readingKey="";scroll=Vector2.zero;
            RefreshTitle();
        }
        void ExitGame()
        {
            ReturnToTitle();
            Application.Quit();
        }
        void OnApplicationQuit()
        {
            if(Active!=this)return;
            if(State!=null) { Save(); RecordCompletion(); }
            screen="Title";
        }
        void Styles()
        {
            if (title != null) return;
            if (font == null) font = Resources.Load<Font>("Fonts/VonwaonBitmap-16px");
            title = new GUIStyle(GUI.skin.label) { font = font, fontSize = 42, wordWrap = true };
            title.normal.textColor = new Color(.85f, .96f, .95f);
            heading = new GUIStyle(title) { fontSize = 25 };
            body = new GUIStyle(title) { fontSize = 22, richText = true };
            small = new GUIStyle(body) { fontSize = 18 };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 20, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(20,20,12,12) };
        }
        void ApplyButtonArt()
        {
            var t=EchoArtTheme.Instance;if(t==null)return;
            button.normal.background=t.button.texture;button.normal.textColor=t.ivory;
            button.hover.background=t.buttonHover.texture;button.hover.textColor=Color.white;
            button.active.background=t.buttonPressed.texture;button.active.textColor=t.ivory;
            button.focused.background=t.buttonHover.texture;
            button.border=new RectOffset(20,20,20,20);
        }
        void Box(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        bool Btn(string text, int height = 62) { bool clicked = GUILayout.Button(new GUIContent(text,EchoStatusTooltip.ExplainTerms(text)), button, GUILayout.Height(height)); if(clicked) EchoAudioFeedback.Button(); return clicked; }
        bool AudioButton(Rect r,string text,GUIStyle style) { bool clicked=GUI.Button(r,text,style); if(clicked) EchoAudioFeedback.Button(); return clicked; }
        string CardText(CardTemplate c) { return c.Name + "  /  " + c.Cost + " 能量\n" + string.Join(" · ", c.Effects.Where(e => !(e is ChapterNoiseDescription)).Select(e => e.GetName())) + (c.NoiseDelta != 0 ? " · 噪声 " + c.NoiseDelta.ToString("+0;-0") : ""); }
        void OnGUI()
        {
            Styles(); ApplyButtonArt();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2, (Screen.height-800*scale)/2,0), Quaternion.identity, Vector3.one*scale);
            if (screen == "Battle")
            {
                EchoChapterPresentation.NoiseMeter(EchoRuntime.PlayerNoise, body);
                Box(new Rect(370, 8, 650, 42), new Color(.025f,.05f,.07f,.95f));
                GUI.Label(new Rect(385,12,620,28), CurrentNode.title + "    |    噪声 " + EchoRuntime.PlayerNoise + " / 100", body);

                return;
            }
            if (help) { DrawHelpScreen(); return; }
            if (DrawStoryScreen()) return;
            var region=screen!="Title"&&CurrentNode!=null?CurrentNode.region:screen!="Title"&&State!=null&&State.act==2?EchoArtRegion.Wasteland:screen!="Title"&&State!=null&&State.act==3?EchoArtRegion.Interference:EchoArtRegion.Ivory;
            EchoChapterPresentation.Backdrop(region,screen=="Title",selectedHero==1);
            if (screen == "Map" && !help && !showDeck)
            {
                DrawTerminalMap();
                return;
            }
            Box(new Rect(42,20,1195,755), new Color(.025f,.03f,.035f,.96f));
            GUILayout.BeginArea(new Rect(62,35,screen=="Title"?720:1155,725));

            if (screen != "Title" && screen != "CharacterSelect") GUILayout.Label(ChapterTitle, title);
            if (State != null && screen != "Title" && screen != "CharacterSelect")
            {
                GUILayout.Label($"生命 {State.hp}/{CurrentHero.Hp}     数据 {State.credits}     记忆 {State.memory}     牌组 {State.deck.Count}", body);
                GUILayout.BeginHorizontal();
                if (Btn(showDeck ? "返回旅程" : "牌组与记录", 40)) { showDeck = !showDeck; scroll = Vector2.zero; }
                if (Btn(help ? "关闭教学" : "玩法说明", 40)) help = !help;
                if (Btn("返回标题",40)) { ReturnToTitle(); }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(16);
            scroll = GUILayout.BeginScrollView(scroll);
            if (help) DrawHelp();
            else if (showDeck) DrawDeck();
            else if (screen == "Title")
            {
                GUILayout.Space(130);
                GUI.enabled = hasContinue;
                if (Btn("继续游戏")) { selection = ""; Resume(); }
                GUI.enabled = true;
                if (Btn("重新游戏")) { screen = "CharacterSelect"; selection = notice = ""; }
                if (Btn("玩法说明")) { helpPage = 0; help = true; }
                if (clearCount>0) GUILayout.Label("已通关 · "+clearCount+" 次",small);
                if (Btn("退出游戏")) ExitGame();
            }
            else if (screen == "CharacterSelect")
            {
                GUILayout.Label("选择人物", heading);
                GUILayout.Space(25);
                GUILayout.BeginHorizontal();
                if(Btn((selectedHero==0?"● ":"")+"仪卫",60)) {selectedHero=0;selection="";}
                GUI.enabled = Data.alternateHero != null;
                if(Btn((selectedHero==1?"● ":"")+"拾荒者",60)) {selectedHero=1;selection="";}
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                var hero = selectedHero == 1 && Data.alternateHero != null ? Data.alternateHero : Data.hero;
                GUILayout.Space(20);
                GUILayout.Label("初始生命：" + hero.Hp, body);
                GUILayout.Space(12);
                GUILayout.Label("初始牌组", heading);
                foreach (var entry in hero.StartingDeck.Entries)
                    GUILayout.Label(entry.NumCopies + " × " + entry.Card.Name, body);
                GUILayout.Space(20);
                if (File.Exists(SavePath)) GUILayout.Label("开始新游戏会覆盖本次旅程的进度，永久档案保留。", small);
                if (Btn(selection == "replace" ? "确认覆盖并开始" : "开始游戏",55))
                {
                    if (!File.Exists(SavePath) || selection == "replace") { NewRun(); selection=""; }
                    else selection="replace";
                }
                if (Btn("返回",45)) {screen="Title";selection=notice="";}
            }
            else if (screen == "Map") DrawMap();
            else if (screen == "Node") DrawNode();
            else if (screen == "Reward")
            {
                GUILayout.Label("战利品", heading);
                GUILayout.Label("选择一张加入牌组",small);
                var effectStyle=new GUIStyle(body){fontSize=19,wordWrap=true,richText=false,alignment=TextAnchor.UpperLeft,padding=new RectOffset(12,12,8,8)};
                var offered=State.reward.Select(Data.FindCard).Where(c=>c!=null).ToArray();
                float effectHeight=115;
                foreach(var c in offered)effectHeight=Mathf.Max(effectHeight,effectStyle.CalcHeight(new GUIContent(RewardEffects(c)),326));
                GUILayout.BeginHorizontal();
                foreach(var card in offered)
                {
                    GUILayout.BeginVertical(GUI.skin.box,GUILayout.Width(350));
                    GUILayout.Label(card.Name + "    " + card.Cost + " 能量",new GUIStyle(heading){fontSize=23},GUILayout.Height(38));
                    var slot=GUILayoutUtility.GetRect(326,210);
                    var rect=new Rect(slot.center.x-105,slot.y,210,210);
                    if(card.Picture!=null)
                    {
                        var sprite=card.Picture;var uv=sprite.textureRect;var texture=sprite.texture;
                        GUI.DrawTextureWithTexCoords(rect,texture,new Rect(uv.x/texture.width,uv.y/texture.height,uv.width/texture.width,uv.height/texture.height));
                    }
                    GUILayout.Label(new GUIContent(RewardEffects(card),EchoStatusTooltip.ExplainTerms(RewardEffects(card))),effectStyle,GUILayout.Height(effectHeight));
                    bool choose=Btn("领取「"+card.Name+"」",42);GUILayout.EndVertical();
                    if(choose){ChooseReward(card.Id);break;}
                }
                GUILayout.EndHorizontal();
                if (Btn("跳过选牌")) ChooseReward(-1);
            }
            else if(screen=="Complete" || screen=="Defeat")
            {
                GUILayout.Label(screen=="Complete"?"全篇终 / "+State.ending:"本次投放终止",heading);
                GUILayout.Space(20);
                GUILayout.Label(screen=="Complete"?State.epilogue:"你的旅程在"+ChapterTitle+"中断了。记录仍留在档案里。重新开始会从第一章出发。",body);
                GUILayout.Space(24);
                if(Btn("回到标题")){ReturnToTitle();}
            }
            if (!string.IsNullOrEmpty(notice)) { GUILayout.Space(15); GUILayout.Label(notice, small); }
            GUILayout.EndScrollView(); GUILayout.EndArea();
            DrawKeywordTooltip();
        }
        string queuedMapVisit;
        void DrawKeywordTooltip()
        {
            if(string.IsNullOrEmpty(GUI.tooltip))return;
            var style=new GUIStyle(body){fontSize=19,wordWrap=true,richText=false,padding=new RectOffset(16,16,12,12)};
            float width=420,height=Mathf.Min(640,style.CalcHeight(new GUIContent(GUI.tooltip),width));
            var mouse=Event.current.mousePosition;
            float x=mouse.x+18;if(x+width>1260)x=mouse.x-width-18;
            var rect=new Rect(Mathf.Clamp(x,10,1270-width),Mathf.Clamp(mouse.y+20,10,790-height),width,height);
            EchoTerminalUI.Fill(rect,new Color(.025f,.04f,.075f,.99f));
            EchoTerminalUI.Fill(new Rect(rect.x,rect.y,3,rect.height),EchoTerminalUI.Cyan);
            GUI.Label(rect,GUI.tooltip,style);
        }
        static string RewardEffects(CardTemplate card)
        {
            var effects=new List<string>();
            for(int i=0;i<card.Effects.Count;i++)
            {
                var e=card.Effects[i];if(e is ChapterNoiseDescription)continue;
                if(e is DealDamageEffect damage)
                {
                    int hits=1;
                    while(i+1<card.Effects.Count && card.Effects[i+1] is DealDamageEffect next && next.Value==damage.Value){hits++;i++;}
                    effects.Add(hits>1?"造成 "+damage.Value+" 点伤害 × "+hits+" 次":e.GetName());
                }
                else effects.Add(e.GetName());
            }
            if(card.NoiseDelta!=0)effects.Add("噪声 "+card.NoiseDelta.ToString("+0;-0"));
            return string.Join("\n",effects);
        }
        public void QueueMapVisit(string id) { queuedMapVisit = id; }
        void Update()
        {
            if (queuedMapVisit == null) return;
            var id = queuedMapVisit; queuedMapVisit = null;
            Visit(id);
        }
        void DrawTerminalMap()
        {
            EchoTerminalUI.Fill(new Rect(100,100,1080,600), EchoTerminalUI.Ink);
            EchoTerminalUI.Label(new Rect(132,120,760,40), ChapterTitle, body, 24, EchoTerminalUI.Text);
            if(EchoTerminalUI.Button(new Rect(1050,120,98,36), "返回", small)) {ReturnToTitle();}
            EchoRouteMap.Draw(new Rect(132,178,1016,490),this,body,small);
        }

        void DrawMap()
        {
            var area = GUILayoutUtility.GetRect(1120, 490);
            EchoRouteMap.Draw(area, this, body, small);
        }
        string readingKey = "";
        int readingPage;
        GUIStyle readingText;
        readonly List<string> readingPages = new List<string>();

        [Serializable] public class StoryCopy
        {
            public string[] introOne, introTwo, introThree, ending, final;
        }
        StoryCopy storyCopy;
        StoryCopy Story
        {
            get
            {
                if (storyCopy == null)
                    storyCopy = JsonUtility.FromJson<StoryCopy>(Resources.Load<TextAsset>("EchoStoryText").text);
                return storyCopy;
            }
        }
        string EndingStory() { return Story.ending[State.act - 1]; }
        string FinalStory(int choice)
        {
            return Story.final[0];
        }
        public string[] IntroText()
        {
            if (State.act == 1) return Story.introOne;
            if (State.act == 2) return Story.introTwo;
            return Story.introThree;
        }

        // Wrap to measured glyph widths and keep six generously spaced lines per page.
        void PrepareReading(string key, string text)
        {
            if (readingKey == key) return;
            readingKey = key; readingPage = 0; readingPages.Clear();
            var lines = new List<string>();
            string line = "";
            foreach (char c in (text ?? "").Replace("\r", ""))
            {
                if (c == '\n') { lines.Add(line); line = ""; continue; }
                if (line.Length > 0 && readingText.CalcSize(new GUIContent(line + c)).x > 760 && !(c == '虎' && line.EndsWith("云")) && !(c == '崽' && line.EndsWith("云虎")) && "，。！？；：、）》”’".IndexOf(c) < 0)
                { lines.Add(line); line = ""; }
                line += c;
            }
            if (line.Length > 0) lines.Add(line);
            for (int i = 0; i < lines.Count; i += 6)
                readingPages.Add(string.Join("\n", lines.Skip(i).Take(6)).Trim());
            if (readingPages.Count == 0) readingPages.Add("记录为空。");
        }

        bool DrawStoryScreen()
        {
            if (State == null) return false;
            bool boss = screen == "Node" && CurrentNode != null && CurrentNode.kind == ChapterNodeKind.Boss;
            if (!boss && screen != "Intro" && screen != "Consequence" && screen != "Ending" && screen != "ChapterComplete" && screen != "Complete") return false;
            if (readingText == null)
            {
                readingText = new GUIStyle(body) { fontSize = 28, richText = true };
                readingText.normal.textColor = new Color(.88f,.86f,.80f);
            }
            string caption = ChapterTitle;
            string text = "";
            if (boss) { caption = CurrentNode.title; text = CurrentNode.story; }
            else if (screen == "Intro")
            {
                var pages = IntroText();
                text = pages[Mathf.Clamp(State.storyPage, 0, pages.Length - 1)];
            }
            else if (screen == "Consequence") { caption = State.resultTitle; text = State.resultText; }
            else if (screen == "Ending") text = EndingStory();
            else if (screen == "ChapterComplete") { caption = State.ending; text = State.journal.LastOrDefault() ?? ""; }
            else { caption = "通关完成 · " + State.ending; text = FinalStory(State.ending != null && State.ending.StartsWith("B") ? 1 : State.ending != null && State.ending.StartsWith("C") ? 2 : 0); }
            PrepareReading(screen + ":" + State.act + ":" + State.nodeId + ":" + State.storyPage + ":" + text, text);
            var matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            Box(new Rect(0,0,Screen.width,Screen.height), new Color(.025f,.028f,.032f,1));
            GUI.matrix = matrix;
            GUI.Label(new Rect(240,70,800,42), caption, small);
            var visibleLines = readingPages[readingPage].Split('\n');
            for (int i = 0; i < visibleLines.Length; i++)
                GUI.Label(new Rect(240,155+i*44,800,44), visibleLines[i].Replace("云虎崽", "<color=#FF4040>云虎崽</color>"), readingText);
            GUI.Label(new Rect(240,458,800,30), (readingPage+1)+" / "+readingPages.Count, small);
            bool next = false;
            // Keyboard advances text only; consequential choices always require an explicit click.
            var ev = Event.current;
            if (ev.type == EventType.KeyDown && (ev.keyCode == KeyCode.Space || ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.RightArrow))
            { next = true; ev.Use(); }
            GUILayout.BeginArea(new Rect(240,505,800,275));
            if (readingPage > 0 && Btn("上一页",38)) readingPage--;
            if (readingPage < readingPages.Count - 1)
            { if (Btn("继续阅读  →",48) || next) readingPage++; }
            else if (boss) { if (Btn("进入战斗",48)) EnterBattle(); }
            else if (screen == "Intro") { if (Btn("继续",48) || next) ContinueIntro(); }
            else if (screen == "Consequence")
            {
                GUILayout.Label(State.resultEffects, small);
                if (Btn("返回路线",48) || next) ContinueConsequence();
            }
            else if (screen == "Ending") DrawChapterEnding();
            else if (screen == "ChapterComplete")
            {
                GUILayout.Label("下一章保留牌组、数据和记忆，恢复全部生命。", small);
                if (Btn("前往下一章",48) || next) BeginNextChapter();
            }
            else if (Btn("通关完成 · 返回主界面",48)) { ReturnToTitle(); }
            if (!string.IsNullOrEmpty(notice)) GUILayout.Label(notice, small);
            GUILayout.EndArea();
            return true;
        }

        void DrawChapterEnding()
        {
            if(State.act==1) { if(Btn("前往档案馆",48))ResolveEnding(false); }
            else if(State.act==2) { if(Btn("前往预测核心",48))ResolveEnding(false); }
            else { if(Btn("服从 AI，返回维修舱",48))ResolveFinal(0); }
        }

        void DrawNode()
        {
            var n = CurrentNode;
            GUILayout.Label(n.title, heading);
            GUILayout.Space(16);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(800));
            GUILayout.Label(n.story,body);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(24);
            if (n.enemy != null)
            {
                GUILayout.Label(n.enemy.Name + "  /  " + n.enemy.HpLow + " HP" + (n.kind == ChapterNodeKind.Elite ? "  /  额外记忆与数据" : ""), body);
                if (Btn("进入战斗")) EnterBattle();
            }
            else if (n.kind == ChapterNodeKind.Event)
            {
                for(int i=0;i<n.choices.Count;i++)
                {
                    var c=n.choices[i]; GUI.enabled = State.credits+c.credits>=0 && State.hp+c.heal>0;
                    if(Btn(c.label,80)) { ChooseEvent(i); break; }
                }
                GUI.enabled=true;
            }
            else if(n.kind == ChapterNodeKind.Rest)
            {
                if(Btn("修复身体  /  回复 " + Data.restHeal + " 生命")) { Rest(); }
                if(Btn("精炼一张牌  /  选择升级，不回复生命")) selection="upgrade";
                if(selection=="upgrade") DrawCardSelection(true);
            }
            else if(n.kind == ChapterNodeKind.Shop)
            {
                var c=Data.rewards[Math.Abs(State.seed % Data.rewards.Count)];
                GUI.enabled=!State.shopBought && State.credits>=Data.cardPrice;
                if(Btn("购买 " + Data.cardPrice + " 数据  /  " + CardText(c),85)) {BuyCard();}
                GUI.enabled=!State.shopRemoved && State.credits>=Data.removePrice && State.deck.Count>5;
                if(Btn("删除一张牌  /  " + Data.removePrice + " 数据")) selection="remove";
                GUI.enabled=true;
                if(selection=="remove") DrawCardSelection(false);
                if(Btn("离开黑市")) LeaveShop();
            }
        }
        void DrawCardSelection(bool upgrade)
        {
            foreach(var id in State.deck.Distinct().ToArray())
            {
                var c=Data.FindCard(id);
                if(upgrade ? c.Upgrade==null : c.IsPermanent) continue;
                if(Btn(upgrade ? c.Name + "  →  " + CardText(c.Upgrade) : "删除 " + c.Name,75))
                {
                    int index=State.deck.IndexOf(id);
                    if(upgrade) { Rest(c.Id); }
                    else { RemoveCard(c.Id); }
                    break;
                }
            }
        }
        void DrawDeck()
        {
            foreach(var group in GetDeck().GroupBy(c=>c.Id)) GUILayout.Label(new GUIContent(group.Count()+" × "+CardText(group.First()),EchoStatusTooltip.ExplainTerms(CardText(group.First()))),body);
            GUILayout.Space(20); GUILayout.Label("旅程记录",heading);
            foreach(var entry in State.journal) {GUILayout.Label(entry.Replace("云虎崽", "<color=#FF4040>云虎崽</color>"),body);GUILayout.Space(12);}
        }
        int helpPage;
        void HelpText(float x, float y, float width, string text, bool large = false)
        {
            GUI.Label(new Rect(x,y,width,large?65:110),text,large?heading:body);
        }
        void HelpCard(Rect r, string name, string description)
        {
            Box(r,new Color(.08f,.12f,.14f));
            Box(new Rect(r.x,r.y,4,r.height),new Color(.35f,.7f,.7f));
            HelpText(r.x+20,r.y+16,r.width-35,name,true);
            HelpText(r.x+20,r.y+75,r.width-35,description);
        }
        void DrawHelpScreen()
        {
            Box(new Rect(0,0,1280,800),new Color(.025f,.03f,.035f,1));
            string[] names={"一回合怎么打", "看意图，决定攻防", "噪声：伤害与风险", "地图与牌组"};
            HelpText(75,35,1100,"玩法说明  /  "+(helpPage+1)+" · "+names[helpPage],true);
            if(helpPage==0)
            {
                HelpText(75,100,1100,"目标：把敌人的生命降到 0，同时保住自己的生命。");
                HelpCard(new Rect(75,185,340,200),"① 回合开始","获得 3 点能量，抽 5 张牌。\n先看敌人头顶：它下一步要做什么？");
                HelpCard(new Rect(465,185,340,200),"② 拖动出牌","攻击牌 → 拖到敌人\n防御牌 → 拖到自己\n每张牌会消耗标出的能量。");
                HelpCard(new Rect(855,185,340,200),"③ 结束回合","点“结束回合”。\n敌人执行头顶的行动。\n随后开始你的下一个回合。");
                HelpText(420,260,45,"→",true);HelpText(810,260,45,"→",true);
                HelpCard(new Rect(75,425,530,185),"能量示例","3 点能量 − 1 费攻击 − 1 费防御\n= 剩余 1 点，还能打一张 1 费牌。");
                HelpCard(new Rect(655,425,540,185),"手牌和抽牌堆","不用把每张牌都打出去。\n弃掉的牌会在抽牌堆用完后重新洗入。\n普通护盾不会一直保留，按当前回合规划。");
            }
            else if(helpPage==1)
            {
                HelpText(75,100,1100,"敌人头顶的图标表示行动类型，旁边的数字表示数值。结束回合后执行。");
                HelpCard(new Rect(75,185,340,195),"攻击 8","敌人下一次攻击造成 8 点伤害。\n先挡住这次攻击，再考虑输出。");
                HelpCard(new Rect(465,185,340,195),"护盾 6","敌人将获得 6 点护盾。\n之后的攻击需要先消耗它的护盾。");
                HelpCard(new Rect(855,185,340,195),"干扰 / 状态","敌人可能增加你的噪声，\n或施加状态。\n把鼠标移到状态图标上查看说明。");
                HelpText(75,420,1100,"例：敌人攻击 8，你获得 5 点护盾",true);
                HelpCard(new Rect(75,485,530,150),"8 点攻击 → 5 点护盾","普通攻击先扣护盾：8 − 5 = 3。");
                HelpCard(new Rect(655,485,540,150),"你失去 3 点生命","如果已获得 8 点护盾，这次普通攻击\n就不会扣生命。穿盾伤害另算。");
            }
            else if(helpPage==2)
            {
                var config=EchoRuntime.Config;
                HelpText(75,100,1100,"噪声是战斗中的风险条。每 10 点提高 "+(config.damageBonusPerTenNoise*100).ToString("0")+"% 伤害，上限按 100 点计算。\n噪声越高，敌人意图越难读；卡牌费用和基础能量不会因此改变。");
                HelpCard(new Rect(75,210,340,185),"0–"+config.stableMax+"  稳定","看见：攻击 8\n可以按准确数字准备护盾。");
                HelpCard(new Rect(465,210,340,185),(config.stableMax+1)+"–"+config.driftMax+"  漂移","看见：攻击 6～10\n只知道范围。想完全挡住，\n就按上限 10 点准备护盾。");
                HelpCard(new Rect(855,210,340,185),(config.driftMax+1)+"–100  崩溃","看见：攻击 ?\n仍知道是攻击，但数值隐藏。\n结束回合穿盾扣 2 点生命。");
                HelpCard(new Rect(75,430,530,210),"怎么增加 / 降低","打出攻击牌：通常 +3 噪声。\n获得每 5 点护盾：降低 2 噪声。\n防御牌还能恢复本回合的精确意图。\n回合结束自然降低，数值由敌人阵营决定。");
                HelpCard(new Rect(655,430,540,210),"例：噪声充能","获得 2 能量，同时增加 25 噪声。\n适合需要多出几张牌的回合。\n防御降噪可以把意图重新看清。\n每场战斗重设噪声；事件可改变初始值。");
            }
            else
            {
                HelpText(75,100,1100,"点击地图上亮起的地点前进。路线决定你能遇到哪些战斗和补给。");
                HelpCard(new Rect(75,185,530,180),"战斗 → 奖励 → 下一个地点","胜利后可选一张牌，也可以跳过。\n牌越多，关键牌越不容易及时抽到。\n精英更危险，但会额外提供记忆与数据。");
                HelpCard(new Rect(655,185,540,180),"休整 / 商店","休整：回复生命或升级一张牌，二选一。\n商店：花数据买牌，或删掉不需要的牌。\n删牌后牌组至少保留 5 张。");
                HelpCard(new Rect(75,410,530,215),"事件 / 记忆","选择按钮会列出收益和代价。\n事件和精英战斗可以获得记忆记录。\n当前固定结局不需要记忆数量。\n精英与 Boss 会针对最近三回合的习惯调整行动。");
                HelpCard(new Rect(655,410,540,215),"保存 / 失败","地点之间自动保存。\n战斗中退出，再继续时从该战开头重来。\n生命归零，本次旅程结束；\n重新游戏从第一章开始。");
            }
            if(AudioButton(new Rect(75,700,220,55),"上一页",button))helpPage=Mathf.Max(0,helpPage-1);
            if(AudioButton(new Rect(325,700,220,55),"下一页",button))helpPage=Mathf.Min(names.Length-1,helpPage+1);
            if(AudioButton(new Rect(945,700,250,55),"关闭说明",button))help=false;
        }
        void DrawHelp() { DrawHelpScreen(); }
    }
}
