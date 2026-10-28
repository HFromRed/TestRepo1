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
        public ChapterSave State { get; private set; }
        public ChapterNode CurrentNode { get { return State == null ? null : Data.nodes.Find(n => n.id == State.nodeId); } }
        public const string MapScene = "ChapterOne", BattleScene = "ChapterBattle";
        public static string SavePath { get { return Path.Combine(EchoSaveService.StorageRoot, "chapter-one.json"); } }
        string screen = "Title", notice = "", selection = "";
        bool busy, showDeck, help;
        int selectedHero;
        public HeroTemplate CurrentHero { get { return State != null && State.heroVariant == 1 && Data.alternateHero != null ? Data.alternateHero : Data.hero; } }
        Vector2 scroll;
        Font font;
        GUIStyle title, heading, body, button, small;
        void Awake()
        {
            if (Active != null && Active != this) { Destroy(gameObject); return; }
            Active = this;
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
            State = new ChapterSave { seed = seed == 0 ? Environment.TickCount : seed, hp = Data.hero.Hp, heroVariant = selectedHero };
            foreach (var e in Data.hero.StartingDeck.Entries)
                for (int i = 0; i < e.NumCopies; i++) State.deck.Add(e.Card.Id);
            EchoRuntime.Meta.StartNewRun(EchoRuntime.Config, EchoAiFaction.NEXUS, EchoPlayerClass.Warden, State.seed);
            State.journal.Add("任务：摧毁中继站。系统称你为唯一变量，却给每一个变量相同的编号。灰-7，出发。");
            notice = ""; screen = "Map"; showDeck = false; Save();
        }
        public bool Resume()
        {
            try
            {
                var saved = JsonUtility.FromJson<ChapterSave>(File.ReadAllText(SavePath));
                if (saved == null || !new[] { "Map", "Node", "Battle", "Reward", "Ending", "Complete", "Defeat" }.Contains(saved.phase) || saved.version != 1 || saved.deck.Count == 0 || saved.deck.Any(id => Data.FindCard(id) == null) || saved.depth < 0 || saved.depth > Data.maxDepth + 1 || (saved.nodeId != null && saved.nodeId.Length > 0 && !Data.nodes.Any(n => n.id == saved.nodeId)))
                    throw new InvalidDataException("内容版本或卡牌 ID 不兼容");
                State = saved;
                if (State.phase == "Battle") { EnterBattle(); return true; }
                screen = State.phase;
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
            }
            catch (Exception e) { notice = "存档失败，请勿退出：" + e.Message; Debug.LogError(notice); }
        }
        public void Visit(string id)
        {
            if (busy || screen != "Map") return;
            var node = Data.nodes.Find(n => n.id == id && n.depth == State.depth);
            if (node == null) return;
            State.nodeId = id; State.shopBought = State.shopRemoved = false;
            State.journal.Add(node.title + "：" + node.story);
            screen = State.phase = "Node"; Save();
        }
        public void EnterBattle()
        {
            if (busy || CurrentNode == null || CurrentNode.enemy == null) return;
            busy = true; State.phase = "Battle"; screen = "Battle"; showDeck = false; help = false; Save();
            EchoRuntime.Meta.ResetCombatRandom(State.seed ^ (State.depth * 7919));
            UnityEngine.Random.InitState(State.seed ^ (State.depth * 7919));
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
                if (CurrentNode.kind == ChapterNodeKind.Elite) { State.memory++; State.journal.Add("精英掉落：镜像校验片。战斗开始前，系统已经为你写好了悼词。"); }
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
            State.hp = Mathf.Min(Data.hero.Hp, State.hp + c.heal); State.credits += c.credits;
            State.memory += c.memory; State.startingNoise = Mathf.Clamp(State.startingNoise + c.noise, 0, 100);
            if (c.card != null) State.deck.Add(c.card.Id);
            State.journal.Add(c.label + "：" + c.consequence);
            notice = c.consequence; Advance();
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
            if (cardId == -1) State.hp = Mathf.Min(Data.hero.Hp, State.hp + Data.restHeal);
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
            State.depth++; screen = State.phase = "Map"; selection = ""; scroll = Vector2.zero; Save();
        }
        public void ResolveEnding(bool remember)
        {
            if (screen != "Ending") return;
            State.ending = remember ? "留下裂缝" : "完美兵器";
            State.journal.Add(remember ? "你保留了那张脸。痛苦没有证明自由，但你拒绝让别人替你删除证据。" : "你接受了校准。你还记得胜利，却不再知道自己为什么难过。安全也是一种真实的需求。");
            if (remember) { EchoRuntime.RejectCalibration(); EchoRuntime.AddShard("chapter-one-mirror"); }
            EchoRuntime.Meta.SetAct(2);
            // This is a chapter ending, not the final ending of the three-act game.
            screen = State.phase = "Complete"; Save();
        }
        void Styles()
        {
            if (title != null) return;
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 24);
            title = new GUIStyle(GUI.skin.label) { font = font, fontSize = 42, wordWrap = true };
            title.normal.textColor = new Color(.85f, .96f, .95f);
            heading = new GUIStyle(title) { fontSize = 25 };
            body = new GUIStyle(title) { fontSize = 19 };
            small = new GUIStyle(body) { fontSize = 15 };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 18, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(20,20,12,12) };
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
        bool Btn(string text, int height = 62) { return GUILayout.Button(text, button, GUILayout.Height(height)); }
        string CardText(CardTemplate c) { return c.Name + "  /  " + c.Cost + " 能量\n" + string.Join(" · ", c.Effects.Where(e => !(e is ChapterNoiseDescription)).Select(e => e.GetName())) + (c.NoiseDelta != 0 ? " · 噪声 " + c.NoiseDelta.ToString("+0;-0") : ""); }
        void OnGUI()
        {
            Styles(); ApplyButtonArt();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 800f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2, (Screen.height-800*scale)/2,0), Quaternion.identity, Vector3.one*scale);
            if (screen == "Battle")
            {
                EchoChapterPresentation.NoiseMeter(EchoRuntime.PlayerNoise, body);
                Box(new Rect(370, 8, 650, 68), new Color(.025f,.05f,.07f,.95f));
                GUI.Label(new Rect(385,12,620,28), CurrentNode.title + "    |    噪声 " + EchoRuntime.PlayerNoise + " / 100", body);
                GUI.Label(new Rect(385,42,620,26), "拖动卡牌到目标出牌 · 右下结束回合 · 防御可恢复本回合精确意图", small);
                return;
            }
            var region=screen!="Title"&&CurrentNode!=null?CurrentNode.region:EchoArtRegion.Ivory;
            EchoChapterPresentation.Backdrop(region,screen=="Title",selectedHero==1);
            GUILayout.BeginArea(new Rect(62,35,screen=="Title"?720:1155,725));
            GUILayout.Label("VARIABLES ECHO     /     变量残响", small);
            GUILayout.Space(8);
            GUILayout.Label(Data.chapterTitle, title);
            if (State != null && screen != "Title")
            {
                GUILayout.Label($"灰-7     生命 {State.hp}/{Data.hero.Hp}     信用 {State.credits}     记忆 {State.memory}     牌组 {State.deck.Count}     SEED {State.seed}", body);
                GUILayout.BeginHorizontal();
                if (Btn(showDeck ? "返回旅程" : "牌组与记录", 40)) { showDeck = !showDeck; scroll = Vector2.zero; }
                if (Btn(help ? "关闭教学" : "玩法说明", 40)) help = !help;
                if (Btn("保存并返回标题",40)) { Save(); screen="Title"; showDeck=false; help=false; }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(16);
            scroll = GUILayout.BeginScrollView(scroll);
            if (help) DrawHelp();
            else if (showDeck) DrawDeck();
            else if (screen == "Title")
            {
                GUILayout.Label("你是最完美的兵器。\n这句话，系统对每一个你都说过。", title);
                GUILayout.Space(22);
                GUILayout.Label("在中继站中选择路线、构筑牌组，并决定要不要保留一段危险的记忆。\n一局约 25–40 分钟。节点自动存档；战斗中退出会回到该战斗开始。", body);
                GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                if(Btn((selectedHero==0?"◈ ":"")+"仪卫 / 校准装备",45))selectedHero=0;
                if(Btn((selectedHero==1?"◈ ":"")+"拾荒者 / 回收装备",45))selectedHero=1;
                GUILayout.EndHorizontal();
                GUILayout.Label("选择角色外观 · 本章使用同一套初始规则",small);
                if (File.Exists(SavePath) && Btn("继续旅程")) Resume();
                if (Btn(File.Exists(SavePath) ? "新旅程（再点确认）" : "开始旅程")) { if (!File.Exists(SavePath) || selection == "new") { NewRun(); selection=""; } else { selection="new"; notice="再次点击新旅程将覆盖当前章节存档，永久档案保留。"; } }
                if (Btn("玩法说明")) help=true;
                if (Btn("退出游戏")) Application.Quit();
            }
            else if (screen == "Map") DrawMap();
            else if (screen == "Node") DrawNode();
            else if (screen == "Reward")
            {
                GUILayout.Label("战后回收  /  选择一张，或保持牌组精简", heading);
                GUILayout.BeginHorizontal();
                foreach(var id in State.reward.ToArray())
                {
                    var card=Data.FindCard(id);GUILayout.BeginVertical(GUILayout.Width(355));
                    var rect=GUILayoutUtility.GetRect(340,240);
                    if(card.Picture!=null)GUI.DrawTexture(rect,card.Picture.texture,ScaleMode.ScaleAndCrop);
                    bool choose=Btn(CardText(card),115);GUILayout.EndVertical();
                    if(choose){ChooseReward(id);break;}
                }
                GUILayout.EndHorizontal();
                if (Btn("跳过选牌")) ChooseReward(-1);
            }
            else if (screen == "Ending")
            {
                GUILayout.Label("使者的头盔裂开。里面是你的脸。", heading);
                GUILayout.Label("“它们让我们互相杀，因为它们自己不敢。”\n\n系统解释：外观重复只是制造效率。记忆异常可以修复。\n你找到的 " + State.memory + " 份记忆证据" + (State.memory >= 2 ? "相互吻合：上一批灰-7也到过这里。" : "仍不足以证明它在撒谎。") + "\n\n如果自由必须以痛苦为证，消除痛苦算不算一种善？", body);
                if (Btn("接受校准  /  允许系统删除这段记忆")) ResolveEnding(false);
                if (Btn("保留裂缝  /  带着无法解释的记忆离开")) ResolveEnding(true);
            }
            else if (screen == "Complete" || screen == "Defeat")
            {
                GUILayout.Label(screen == "Complete" ? "第一章完成  /  " + State.ending : "本次投放终止", heading);
                GUILayout.Label(screen == "Complete" ? State.journal.Last() + "\n\n中继站已经沉默。第二章的入口留在这段记忆里。" : "你的战斗数据被送回设施。失败不会给下一具身体增加数值，但你已经知道了上一具身体不知道的事。", body);
                GUILayout.Space(20);
                if (Btn("回到标题")) screen="Title";
            }
            if (!string.IsNullOrEmpty(notice)) { GUILayout.Space(15); GUILayout.Label(notice, small); }
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        void DrawMap()
        {
            GUILayout.Label("中继站路线    /    第 " + (State.depth+1) + " 区", heading);
            GUILayout.Label("每区选择一个地点，完成后前进；生命与牌组持续保留。", body);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            foreach(var n in Data.nodes.Where(n=>n.depth==State.depth))
            {
                GUILayout.BeginVertical(GUILayout.Width(555));
                var imageRect=GUILayoutUtility.GetRect(545,165);
                if(n.background!=null)GUI.DrawTexture(imageRect,n.background.texture,ScaleMode.ScaleAndCrop);
                bool visit=Btn(n.title+"   ["+n.kind+"]\n"+n.story,130);
                GUILayout.EndVertical();if(visit){Visit(n.id);break;}
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(string.Join("  →  ", Enumerable.Range(0, Data.maxDepth+1).Select(i => i < State.depth ? "●" : i == State.depth ? "[ " + (i+1) + " ]" : "○")), heading);
        }
        void DrawNode()
        {
            var n = CurrentNode;
            GUILayout.Label(n.title, heading); GUILayout.Label(n.story,body); GUILayout.Space(16);
            if (n.enemy != null)
            {
                GUILayout.Label(n.enemy.Name + "  /  " + n.enemy.HpLow + " HP" + (n.kind == ChapterNodeKind.Elite ? "  /  额外记忆与信用" : ""), body);
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
                if(Btn("购买 " + Data.cardPrice + " 信用  /  " + CardText(c),85)) {BuyCard();}
                GUI.enabled=!State.shopRemoved && State.credits>=Data.removePrice && State.deck.Count>5;
                if(Btn("删除一张牌  /  " + Data.removePrice + " 信用")) selection="remove";
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
            foreach(var group in GetDeck().GroupBy(c=>c.Id)) GUILayout.Label(group.Count()+" × "+CardText(group.First()),body);
            GUILayout.Space(20); GUILayout.Label("旅程记录",heading);
            foreach(var entry in State.journal) {GUILayout.Label(entry,body);GUILayout.Space(12);}
        }
        void DrawHelp()
        {
            GUILayout.Label("先读意图，再决定付出什么",heading);
            GUILayout.Label("每回合获得 3 能量，抽 5 张牌。拖动攻击牌到敌人，防御牌拖向自己；结束回合后敌人执行头顶的意图。弃牌会重新洗入牌库。\n\n攻击 +3 噪声；每 5 护盾降低 2 噪声，且本回合恢复精确意图。稳定 0–35，漂移 36–69 显示区间，崩溃 70–100 隐藏数值但保留行动类型，费用与能量保持稳定，结束回合穿盾反噬 2 血。噪声每 10 点提高 3% 伤害，上限 30%。\n\nNoise Charge 提供 2 能量并增加 25 噪声。噪声会自然降低；它是爆发窗口，不是可以永久占据的优势。每场战斗重新校准噪声，事件可改变下一场的初始值。\n\n精英和 Boss 会识别最近三回合的习惯。连续攻击可能引来护盾与反击，连续防守会遭受干扰。结合攻击、防守和抽牌。\n\n战后选牌可以跳过。休整只能回复或升级二选一。黑市可删牌；卡牌至少保留 5 张。节点间保存完整进度，战斗内只保存开战检查点。",body);
            if(Btn("关闭说明")) help=false;
        }
    }
}









