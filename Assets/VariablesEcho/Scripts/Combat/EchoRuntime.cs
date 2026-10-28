using System.Collections.Generic;
using CCGKit;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VariablesEcho
{
    /// <summary>
    /// 框架的静态入口。CCG Kit 的少量改造点会调用这里。
    /// </summary>
    public static class EchoRuntime
    {
        // 每打出一张攻击牌的噪声增量。
        private const int AttackNoise = 3;
        // 每累计 5 点护盾降低 2 点噪声。
        private const int ShieldPerNoiseTick = 5;
        private const int NoisePerShieldTick = 2;

        public static EchoConfig Config { get; private set; }
        public static EchoMetaService Meta { get; private set; }
        public static EchoBattleContext Battle { get; private set; }

        public static EchoRngService Rng
        {
            get { return Meta != null ? Meta.Rng : null; }
        }

        public static bool IsBattleActive
        {
            get { return Battle != null; }
        }

        public static int PlayerNoise
        {
            get { return Battle != null ? Battle.Noise.Value : 0; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            EnsureInitialized();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public static void EnsureInitialized()
        {
            if (Config == null)
                Config = EchoConfig.Instance;
            if (Meta == null)
                Meta = EchoMetaService.LoadOrCreate();
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            Battle = null;
        }

        /// <summary>由 GameBootstrap.InitializeGame 调用（战斗初始化完成后、BeginGame 之前）。</summary>
        public static void OnBattleInitialized(CharacterObject player, List<CharacterObject> enemies)
        {
            EnsureInitialized();
            Meta.EnsureRun(Config);
            Meta.OnBattleStarted();

            var fallbackOpponent = Meta.Run != null ? Meta.Run.opponent : Config.defaultOpponent;
            Battle = new EchoBattleContext(
                player,
                enemies ?? new List<CharacterObject>(),
                new NoiseController(Config),
                new BehaviorTagger(),
                fallbackOpponent);

            var brainSystem = UnityEngine.Object.FindObjectOfType<EnemyBrainSystem>();
            if (brainSystem != null)
                brainSystem.IntentProvider = Battle.IntentProvider;

            if (Config.showDebugHud && EchoChapterController.Active == null)
                EchoDebugHud.CreateIfNeeded();

            EchoStatusTooltip.Install();
            EchoFloatingText.Install();
            EchoDistortion.Install();

            EchoEvents.RaiseBattleStarted(Battle);
        }

        public static void OnPlayerTurnBegan()
        {
            if (Battle == null)
                return;

            Battle.Tagger.BeginPlayerTurn();
            EchoDistortion.OnPlayerTurnBegan();
        }

        public static void OnPlayerTurnEnded()
        {
            if (Battle == null)
                return;

            var opponent = Battle.Opponent;

            // === Variables Echo integration ===
            // 系统自恢复：档位越高，回合结束时回落得越快。
            // 这样"崩溃"是一个短暂的高压窗口，而不是一旦进入就永久烂在里面：
            // 继续攻击可以把它按在崩溃档，停手就会迅速降温。
            var tierBeforeDecay = Battle.Noise.Tier;

            // 崩溃档反噬：噪声顶峰会在回合结束穿透护盾扣血，
            // 让"高噪声 = 白嫖伤害加成"不成立，逼玩家在输出和降温之间做选择。
            if (tierBeforeDecay == EchoNoiseTier.Collapse)
                ApplyCollapseBacklash();

            if (Battle == null) return; // Lethal backlash may have ended the battle synchronously.
            Battle.Noise.EndTurn(opponent);
            if (tierBeforeDecay == EchoNoiseTier.Collapse)
                Battle.Noise.Add(-8, "self-recover");
            else if (tierBeforeDecay == EchoNoiseTier.Drift)
                Battle.Noise.Add(-2, "self-recover");
            // === End Variables Echo integration ===

            Battle.Tagger.EndPlayerTurn();
            EchoEvents.RaiseBehaviorTagChanged(Battle.Tagger.Evaluate());

            // 玩家回合结束：玩家身上的临时状态递减
            EchoStatus.TickDecay(Battle.PlayerRuntime);
        }

        /// <summary>崩溃档回合结束的噪声反噬：直接扣血，不经过护盾。</summary>
        private static void ApplyCollapseBacklash()
        {
            var player = Battle.PlayerRuntime;
            if (player == null || player.Hp == null)
                return;

            var amount = EchoDistortion.CollapseBacklash;
            var hp = Mathf.Max(0, player.Hp.Value - amount);
            player.Hp.SetValue(hp);

            // 复用反伤飘字通道，在玩家头顶弹出青色 -2。
            EchoEvents.RaiseReflected(player, amount);
        }

        public static void OnEnemyTurnBegan()
        {
        }

        public static void OnEnemyTurnEnded()
        {
            if (Battle == null)
                return;

            // 敌人回合结束：敌人身上的临时状态递减（虚弱在影响过敌人行动之后才消失）
            foreach (var enemy in Battle.Enemies)
            {
                if (enemy != null)
                    EchoStatus.TickDecay(enemy.Character);
            }
        }

        /// <summary>由 CardSelectionSystem.PlaySelectedCard 在效果结算前调用。</summary>
        public static void OnCardPlayed(CardTemplate card)
        {
            if (card == null || Battle == null)
                return;

            var record = EchoCardClassifier.Describe(card);
            Battle.Tagger.Record(record);

            if (card.NoiseDelta != 0)
                Battle.Noise.Add(card.NoiseDelta, record.CardId);

            // 攻击是"发声"行为：每次打出带伤害的牌都会推高噪声，
            // 与防御降噪一起构成"贪输出 / 保清醒"的核心取舍。
            if (record.IsAttack)
                Battle.Noise.Add(AttackNoise, "attack");

            if (Meta != null)
                Meta.RecordCardPlayed(card);

            EchoEvents.RaiseCardPlayed(card, record);
        }

        /// <summary>由 CharacterDeathSystem.EndGame 调用。</summary>
        public static void OnBattleEnded(bool playerDied)
        {
            if (Battle == null)
                return;

            if (playerDied)
            {
                if (Meta != null)
                    Meta.FinalizeRun(false, EchoEndingId.None, Config);
            }
            else if (Meta != null && Meta.Run != null && Meta.Run.finalBattle)
            {
                // 最终 BOSS 战胜利：按路线/污染/碎片/新卡条件结算结局。
                Meta.FinalizeRun(true, EchoEndingId.None, Config);
            }
            else if (Meta != null)
            {
                Meta.RecordBattleWon();
            }

            EchoEvents.RaiseBattleEnded(playerDied);
            Battle = null;
        }

        public static string StableId(CardTemplate card)
        {
            if (card == null)
                return "";
            if (!string.IsNullOrEmpty(card.CardId))
                return card.CardId;
            return card.Id.ToString();
        }

        public static void AddNoise(int delta)
        {
            if (Battle != null)
                Battle.Noise.Add(delta, "effect");
        }

        /// <summary>由 GainShieldEffect 调用：玩家防守会降低噪声。</summary>
        public static void OnShieldGained(RuntimeCharacter target, int amount)
        {
            if (Battle == null || amount <= 0)
                return;
            if (!Battle.IsPlayer(target))
                return;

            var reduction = (amount / ShieldPerNoiseTick) * NoisePerShieldTick;
            if (reduction > 0)
                Battle.Noise.Add(-reduction, "shield");

            // 防守校准：举起护盾意味着把注意力收回来，本回合剩余时间里意图恢复精确显示。
            // 这是玩家面对"意图不确定"时唯一主动获取情报的手段，代价是一点法力。
            EchoDistortion.RevealThisTurn();
        }

        public static void AddPredictLocks(RuntimeCharacter target, int amount)
        {
            if (Battle != null)
                Battle.AddPredictLocks(target, amount);
        }

        public static void BeginRoute(EchoRoute route)
        {
            if (Meta != null)
                Meta.SetRoute(route);
        }

        public static void SetAct(int act)
        {
            if (Meta != null)
                Meta.SetAct(act);
        }

        public static void MarkFinalBattle(bool value = true)
        {
            if (Meta != null)
                Meta.MarkFinalBattle(value);
        }

        public static void AddShard(string id)
        {
            if (Meta != null)
                Meta.AddResonanceShard(id);
        }

        public static void RejectCalibration()
        {
            if (Meta != null)
                Meta.RecordCalibrationRejected();
        }

        public static EchoEndingId FinishRun(bool victory, EchoEndingId forcedEnding = EchoEndingId.None)
        {
            return Meta != null
                ? Meta.FinalizeRun(victory, forcedEnding, Config)
                : EchoEndingId.None;
        }
    }
}


