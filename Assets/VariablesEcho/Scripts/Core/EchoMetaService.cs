using System;
using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 元进度与跑图状态服务。全局唯一实例由 EchoRuntime 持有。
    /// </summary>
    public sealed class EchoMetaService
    {
        public EchoMetaProfile Profile { get; private set; }
        public EchoRunState Run { get; private set; }
        public EchoRngService Rng { get; private set; }

        private EchoMetaService()
        {
        }

        public static EchoMetaService LoadOrCreate()
        {
            var service = new EchoMetaService();
            service.Load();
            return service;
        }

        private void Load()
        {
            if (!EchoSaveService.TryRead(EchoSaveService.MetaPath, out EchoMetaProfile profile))
            {
                profile = new EchoMetaProfile();
                profile.resonancePollution = EchoConfig.Instance.initialPollution;
            }

            Profile = profile;

            if (EchoSaveService.TryRead(EchoSaveService.RunPath, out EchoRunState run))
            {
                Run = run;
                Rng = new EchoRngService(run.seed);
            }
        }

        public void ResetCombatRandom(int seed)
        {
            Rng = new EchoRngService(seed);
        }
        public void SaveMeta()
        {
            EchoSaveService.Write(EchoSaveService.MetaPath, Profile);
        }

        public void SaveRun()
        {
            if (Run != null)
                EchoSaveService.Write(EchoSaveService.RunPath, Run);
        }

        public bool EnsureRun(EchoConfig config)
        {
            if (Run != null)
            {
                if (Rng == null)
                    Rng = new EchoRngService(Run.seed);
                return true;
            }

            if (config == null || !config.autoStartRun)
                return false;

            StartNewRun(config, config.defaultOpponent, config.defaultPlayerClass, 0);
            return true;
        }

        public void StartNewRun(EchoConfig config, EchoAiFaction opponent, EchoPlayerClass playerClass, int seed)
        {
            if (seed == 0)
                seed = unchecked((int)DateTime.UtcNow.Ticks);

            Run = new EchoRunState
            {
                seed = seed,
                opponent = opponent,
                playerClass = playerClass,
                route = EchoRoute.None,
                act = 1,
                finalBattle = false
            };

            Rng = new EchoRngService(seed);
            SaveRun();
            EchoEvents.RaiseRunStarted(Run);
        }

        /// <summary>每场战斗开始时调用，重置本场统计。</summary>
        public void OnBattleStarted()
        {
            if (Run == null)
                return;

            Run.newCardsPlayedThisBattle = 0;
            SaveRun();
        }

        /// <summary>记录一张打出的牌。返回该牌是否是档案中的首次出现。</summary>
        public bool RecordCardPlayed(CardTemplate card)
        {
            if (card == null)
                return false;

            var id = EchoRuntime.StableId(card);
            if (string.IsNullOrEmpty(id))
                return false;

            var isNew = !Profile.uniqueCardsPlayed.Contains(id);
            if (isNew)
                Profile.uniqueCardsPlayed.Add(id);

            if (card.IsNoiseCard)
            {
                Profile.noiseCardsPlayed++;
                if (Run != null)
                    Run.noiseCardsPlayed++;
            }

            if (card.IsSelfHarm)
            {
                Profile.selfHarmCount++;
                if (Run != null)
                    Run.selfHarmCount++;
            }

            if (Run != null)
            {
                if (!Run.cardsPlayedThisRun.Contains(id))
                    Run.cardsPlayedThisRun.Add(id);

                if (isNew)
                    Run.newCardsPlayedThisBattle++;
            }

            return isNew;
        }

        public void RecordCalibrationRejected()
        {
            Profile.calibrationRejected++;
            if (Run != null)
                Run.calibrationRejectedThisRun++;
        }

        public void AddResonanceShard(string id)
        {
            if (!string.IsNullOrEmpty(id) && !Profile.memoryFragments.Contains(id))
                Profile.memoryFragments.Add(id);

            if (Run != null)
                Run.shardsThisRun++;
        }

        public void RecordBattleWon()
        {
            if (Run == null)
                return;

            Run.battlesWon++;
            SaveMeta();
            SaveRun();
        }

        public void SetRoute(EchoRoute route)
        {
            if (Run == null)
                return;

            Run.route = route;
            Profile.lastRoute = route.ToString();
            SaveRun();
        }

        public void SetAct(int act)
        {
            if (Run == null)
                return;

            Run.act = Mathf.Max(1, act);
            SaveRun();
        }

        public void MarkFinalBattle(bool value)
        {
            if (Run == null)
                return;

            Run.finalBattle = value;
            SaveRun();
        }

        /// <summary>结算当前跑图并删除 run.json。返回最终结局。</summary>
        public EchoEndingId FinalizeRun(bool victory, EchoEndingId forcedEnding, EchoConfig config)
        {
            if (Run == null)
                return EchoEndingId.None;

            var cfg = config ?? EchoConfig.Instance;
            var ending = forcedEnding != EchoEndingId.None
                ? forcedEnding
                : EndingResolver.Evaluate(Run, Profile, victory);

            var pollutionGain =
                Run.noiseCardsPlayed * cfg.pollutionPerNoiseCard +
                Run.calibrationRejectedThisRun * cfg.pollutionPerRejectedCalibration +
                Run.shardsThisRun * cfg.pollutionPerShard;

            if (pollutionGain > cfg.maxPollutionPerRun)
                pollutionGain = cfg.maxPollutionPerRun;
            if (pollutionGain < 0f)
                pollutionGain = 0f;

            Profile.resonancePollution = Mathf.Clamp(Profile.resonancePollution + pollutionGain, 0f, 100f);
            Profile.totalRuns++;
            if (victory)
                Profile.totalVictories++;

            if (ending != EchoEndingId.None)
            {
                var key = ending.ToString();
                if (!Profile.unlockedEndings.Contains(key))
                    Profile.unlockedEndings.Add(key);
                Profile.lastEndingId = key;
            }

            SaveMeta();

            if (cfg.clearKitProgressOnRunEnd && EchoChapterController.Active == null)
                EchoSaveService.ClearKitProgress();

            EchoSaveService.Delete(EchoSaveService.RunPath);
            Run = null;
            Rng = null;

            EchoEvents.RaiseRunEnded(ending);
            return ending;
        }

        public EchoEndingId EvaluateEndingNow(bool victory)
        {
            return EndingResolver.Evaluate(Run, Profile, victory);
        }
    }
}

