using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 变量残响框架的全局数值配置。
    /// 可以通过菜单 Variables Echo/Create Config Asset 创建到
    /// Assets/VariablesEcho/Resources/EchoConfig.asset；没有资产时使用代码内默认值。
    /// </summary>
    [CreateAssetMenu(menuName = "Variables Echo/Config", fileName = "EchoConfig")]
    public class EchoConfig : ScriptableObject
    {
        [Header("Noise / 噪声")]
        [Tooltip("噪声上限。")]
        public int maxNoise = 100;

        [Tooltip("0 ~ stableMax 为稳定档。")]
        public int stableMax = 35;

        [Tooltip("stableMax+1 ~ driftMax 为漂移档；更高为崩溃档。")]
        public int driftMax = 69;

        [Tooltip("被标记的阈值（回合结束衰减后判定）。")]
        public int trackedThreshold = 70;

        [Tooltip("连续多少回合结束时达到阈值会被标记。")]
        public int trackedTurnThreshold = 2;

        [Tooltip("OMEGA 对手造成的每回合自然衰减。")]
        public int omegaDecay = 12;

        [Tooltip("NEXUS 对手造成的每回合自然衰减。")]
        public int nexusDecay = 5;

        [Tooltip("未指定阵营时的默认衰减。")]
        public int defaultDecay = 8;

        [Tooltip("每 10 点噪声提升的伤害比例。")]
        public float damageBonusPerTenNoise = 0.03f;

        [Tooltip("被标记时，正向噪声增量是否减半。")]
        public bool halvePositiveNoiseWhenTracked = true;

        [Header("Intent / 意图预测")]
        [Tooltip("被标记时敌人意图的锁定层数。")]
        public int maxPredictLocks = 3;

        [Tooltip("漂移档时按置信度选择“克制行动”的概率。")]
        public bool useConfidenceForDrift = true;

        [Tooltip("把锁定层数同步为角色 Status（会显示在 Kit 的状态栏）。")]
        public bool applyPredictLockStatus = false;

        [Tooltip("创建运行时调试 HUD。")]
        public bool showDebugHud = true;

        [Header("Meta / 元进度")]
        [Tooltip("新档案的初始污染度。")]
        public float initialPollution = 12f;

        [Tooltip("每打出 1 张噪声牌结算的污染度。")]
        public float pollutionPerNoiseCard = 0.05f;

        [Tooltip("每次拒绝校准结算的污染度。")]
        public float pollutionPerRejectedCalibration = 0.3f;

        [Tooltip("每枚残响碎片结算的污染度。")]
        public float pollutionPerShard = 0.4f;

        [Tooltip("单次跑图结算的污染度上限。")]
        public float maxPollutionPerRun = 3f;

        [Tooltip("跑图结束（死亡/通关）时清除 Kit 的 PlayerPrefs 进度。")]
        public bool clearKitProgressOnRunEnd = true;

        [Header("Run / 跑图")]
        [Tooltip("战斗开始时若没有存档，是否自动开始一局新跑图。")]
        public bool autoStartRun = true;

        public EchoAiFaction defaultOpponent = EchoAiFaction.NEXUS;
        public EchoPlayerClass defaultPlayerClass = EchoPlayerClass.Warden;

        private static EchoConfig cached;

        public static EchoConfig Instance
        {
            get
            {
                if (cached != null)
                    return cached;

                cached = Resources.Load<EchoConfig>("EchoConfig");
                if (cached == null)
                {
                    cached = CreateInstance<EchoConfig>();
                    cached.name = "EchoConfig (runtime default)";
                    cached.hideFlags = HideFlags.DontSave;
                }

                return cached;
            }
        }

        public int DecayFor(EchoAiFaction faction)
        {
            switch (faction)
            {
                case EchoAiFaction.OMEGA:
                    return omegaDecay;
                case EchoAiFaction.NEXUS:
                    return nexusDecay;
                default:
                    return defaultDecay;
            }
        }

        public EchoNoiseTier TierFor(int value)
        {
            if (value <= stableMax)
                return EchoNoiseTier.Stable;
            if (value <= driftMax)
                return EchoNoiseTier.Drift;
            return EchoNoiseTier.Collapse;
        }
    }
}
