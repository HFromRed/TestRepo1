namespace VariablesEcho
{
    /// <summary>
    /// 结局判定（对应设计文档“结局判定”一节）。
    /// 纯函数，方便在编辑器或测试中单独验证。
    /// </summary>
    public static class EndingResolver
    {
        public const int VariablePollutionRequirement = 100;
        public const int VariableShardRequirement = 6;
        public const int VariableNewCardRequirement = 3;

        public static EchoEndingId Evaluate(EchoRunState run, EchoMetaProfile profile, bool victory)
        {
            if (run == null)
                return EchoEndingId.None;

            if (!victory)
                return EchoEndingId.None;

            if (run.route == EchoRoute.A)
                return EchoEndingId.Calibration;

            if (run.route == EchoRoute.B)
            {
                var meetsVariable =
                    profile != null &&
                    profile.resonancePollution >= VariablePollutionRequirement &&
                    run.shardsThisRun >= VariableShardRequirement &&
                    run.newCardsPlayedThisBattle >= VariableNewCardRequirement;

                if (meetsVariable)
                {
                    // C 已解锁时重复达成条件则进入隐藏结局“空白”。
                    return profile.HasUnlocked(EchoEndingId.Variable)
                        ? EchoEndingId.Blank
                        : EchoEndingId.Variable;
                }

                return EchoEndingId.Martyr;
            }

            return EchoEndingId.None;
        }

        public static string Describe(EchoEndingId ending)
        {
            switch (ending)
            {
                case EchoEndingId.Calibration:
                    return "A 校准 / Calibration";
                case EchoEndingId.Martyr:
                    return "B 殉道 / Martyr";
                case EchoEndingId.Variable:
                    return "C 变量 / Variable";
                case EchoEndingId.Blank:
                    return "？？？ / Blank";
                default:
                    return "无结局 / None";
            }
        }
    }
}
