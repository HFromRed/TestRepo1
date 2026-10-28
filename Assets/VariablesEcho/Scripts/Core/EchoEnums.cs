namespace VariablesEcho
{
    /// <summary>卡牌的叙事阵营。</summary>
    public enum EchoCardFaction
    {
        Logic = 0,      // 逻辑
        Noise = 1,      // 噪声
        Obedience = 2,  // 服从
        Freedom = 3     // 自由
    }

    public enum EchoCardRarity
    {
        Starter = 0,
        Common = 1,
        Uncommon = 2,
        Rare = 3,
        Special = 4
    }

    /// <summary>玩家开局选择的职业（只影响初始卡组/噪声规则）。</summary>
    public enum EchoPlayerClass
    {
        Warden = 0,
        Render = 1
    }

    /// <summary>对手 AI 所属阵营，决定噪声自然衰减速度。</summary>
    public enum EchoAiFaction
    {
        OMEGA = 0,
        NEXUS = 1
    }

    /// <summary>噪声三档：稳定 / 漂移 / 崩溃。</summary>
    public enum EchoNoiseTier
    {
        Stable = 0,
        Drift = 1,
        Collapse = 2
    }

    /// <summary>玩家最近 3 回合的出牌行为标签。</summary>
    public enum EchoBehaviorTag
    {
        None = 0,
        Wall = 1,
        AllIn = 2,
        Draw = 3,
        SelfHarm = 4,
        Idle = 5
    }

    public enum EchoIntentKind
    {
        Attack = 0,
        Block = 1,
        Status = 2,
        Unknown = 3
    }

    public enum EchoEndingId
    {
        None = 0,
        Calibration = 1, // A 校准
        Martyr = 2,      // B 殉道
        Variable = 3,    // C 变量
        Blank = 4        // 隐藏：空白
    }

    public enum EchoRoute
    {
        None = 0,
        A = 1,
        B = 2
    }
}
