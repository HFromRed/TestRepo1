using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using VariablesEcho;

namespace VariablesEchoEditor
{
    /// <summary>
    /// 噪声失真规则的自测与玩法模拟。
    /// 菜单：Variables Echo > Test > Run Noise Distortion Test
    /// 也可以批处理调用：-executeMethod VariablesEchoEditor.EchoDistortionSelfTest.Run
    /// </summary>
    public static class EchoDistortionSelfTest
    {
        [MenuItem("Variables Echo/Test/Run Noise Distortion Test")]
        public static void Run()
        {
            var report = new StringBuilder();
            report.AppendLine("=== 规则断言 ===");
            RunAssertions(report);

            report.AppendLine();
            report.AppendLine("=== 玩法模拟（12 回合，两种策略）===");
            Simulate(report, "全力输出", aggressive: true);
            Simulate(report, "攻守平衡", aggressive: false);

            Debug.Log(report.ToString());
        }

        private static void RunAssertions(StringBuilder report)
        {
            var fake = Sprite.Create(new Texture2D(4, 4), new Rect(0, 0, 4, 4), Vector2.one * 0.5f);

            Sprite sprite;
            string text;
            var failed = 0;

            // 稳定档：数值必须精确
            EchoDistortion.DistortIntent(EchoNoiseTier.Stable, fake, 9, out sprite, out text);
            failed += Check(report, text == "9", "稳定档应显示精确值 9，实际 " + text);

            // 漂移档：区间必须包含真实值
            foreach (var value in new[] { 4, 6, 8, 9, 12 })
            {
                EchoDistortion.DistortIntent(EchoNoiseTier.Drift, fake, value, out sprite, out text);
                var parts = text.Split('~');
                var low = int.Parse(parts[0]);
                var high = int.Parse(parts[1]);
                failed += Check(report, low <= value && value <= high,
                    string.Format("漂移档 {0} 的区间 {1} 未包含真实值", value, text));
                failed += Check(report, low >= 0, "漂移档区间下界不能为负：" + text);
                report.AppendLine(string.Format("  真实 {0} -> 显示 {1}", value, text));
            }

            // 崩溃档：完全隐藏
            EchoDistortion.DistortIntent(EchoNoiseTier.Collapse, fake, 9, out sprite, out text);
            failed += Check(report, text == "?" && sprite == fake, "崩溃档应隐藏数值并保留行动图标，实际 " + text);

            // 费用偏移分布
            var counts = new int[3];
            for (var i = 0; i < 3000; i++)
                counts[EchoDistortion.RollCostOffset(2, EchoNoiseTier.Collapse) + 1]++;
            report.AppendLine(string.Format("  崩溃档 2 费牌偏移分布：-1={0}  0={1}  +1={2}",
                counts[0], counts[1], counts[2]));
            failed += Check(report, counts[1] == 3000, "费用偏移分布统计异常");

            // 0 费牌不允许变成负数
            var zeroBad = 0;
            for (var i = 0; i < 1000; i++)
            {
                if (EchoDistortion.RollCostOffset(0, EchoNoiseTier.Collapse) < 0)
                    zeroBad++;
            }
            failed += Check(report, zeroBad == 0, "0 费牌出现了负偏移：" + zeroBad);

            // 法力浮动范围
            var manaCounts = new Dictionary<int, int>();
            for (var i = 0; i < 3000; i++)
            {
                var mana = EchoDistortion.RollMana(3, EchoNoiseTier.Collapse);
                if (!manaCounts.ContainsKey(mana))
                    manaCounts[mana] = 0;
                manaCounts[mana]++;
                if (mana != 3)
                    failed++;
            }
            report.AppendLine(string.Format("  崩溃档法力分布（应全部保持 3）：2={0}  3={1}  4={2}",
                Get(manaCounts, 2), Get(manaCounts, 3), Get(manaCounts, 4)));

            // 非崩溃档必须稳定
            failed += Check(report, EchoDistortion.RollMana(3, EchoNoiseTier.Stable) == 3 &&
                                      EchoDistortion.RollMana(3, EchoNoiseTier.Drift) == 3,
                "稳定/漂移档法力不应浮动");

            if (failed != 0) throw new Exception("Noise assertions failed: " + failed);
            report.AppendLine(failed == 0 ? "结果：全部通过" : string.Format("结果：{0} 项失败", failed));
        }

        private static int Get(Dictionary<int, int> map, int key)
        {
            int value;
            return map.TryGetValue(key, out value) ? value : 0;
        }

        private static int Check(StringBuilder report, bool condition, string message)
        {
            if (!condition)
                report.AppendLine("  [FAIL] " + message);
            return condition ? 0 : 1;
        }

        // ------------------------------------------------------------------
        // 玩法模拟：用一副简化卡组跑两种策略，观察噪声轨迹与失真影响
        // ------------------------------------------------------------------

        private const int StrikeCount = 5;
        private const int DefendCount = 4;
        private const int BashCount = 1;
        private const int NoiseCount = 2;
        private const int Turns = 12;

        private struct SimCard
        {
            public string Name;
            public int Cost;
            public int Damage;
            public int Shield;
            public int Noise;
            public int Mana;
        }

        private static void Simulate(StringBuilder report, string label, bool aggressive)
        {
            var random = new System.Random(20261027);
            var drawPile = new List<SimCard>();
            var hand = new List<SimCard>();
            var handOffsets = new List<int>();
            var discard = new List<SimCard>();

            for (var i = 0; i < StrikeCount; i++)
                drawPile.Add(new SimCard { Name = "Strike", Cost = 1, Damage = 6, Noise = 3 });
            for (var i = 0; i < DefendCount; i++)
                drawPile.Add(new SimCard { Name = "Defend", Cost = 1, Shield = 5, Noise = -2 });
            for (var i = 0; i < BashCount; i++)
                drawPile.Add(new SimCard { Name = "Bash", Cost = 2, Damage = 8, Noise = 3 });
            for (var i = 0; i < NoiseCount; i++)
                drawPile.Add(new SimCard { Name = "NoiseCharge", Cost = 0, Noise = 25, Mana = 2 });

            var noise = 0;
            var totalDamage = 0;
            var distortedTurns = 0;
            var chaosTurns = 0;
            var overflowMana = 0;
            var lockedTurns = 0;

            // 近似模拟一场真实战斗：玩家 80 HP，敌人 85 HP + 护盾，敌人按三张牌循环行动。
            var playerHp = 80;
            var playerShield = 0;
            var enemyHp = 85;
            var enemyShield = 0;
            var enemyStep = 0; // 0: 9 伤害 1: 6 护盾 + 玩家 +5 噪声 2: 4 伤害 + 1 虚弱
            var killTurn = 0;
            var deathTurn = 0;

            report.AppendLine();
            report.AppendLine("[" + label + "]");

            for (var turn = 1; turn <= Turns; turn++)
            {
                var tier = TierFor(noise);
                var manaCap = EchoDistortion.RollMana(3, tier);
                var mana = manaCap;

                // 抽牌时掷一次费用偏移（崩溃档才可能非 0），之后这张牌的费用固定不变。
                Draw(drawPile, discard, hand, handOffsets, 5, random, tier);

                if (tier == EchoNoiseTier.Drift)
                    distortedTurns++;
                if (tier == EchoNoiseTier.Collapse)
                    chaosTurns++;

                var played = 0;

                while (true)
                {
                    var pick = -1;
                    var bestScore = 0;

                    for (var i = 0; i < hand.Count; i++)
                    {
                        var card = hand[i];
                        var cost = Mathf.Max(0, card.Cost + handOffsets[i]);
                        if (cost > mana)
                            continue;

                        var score = Score(card, noise, aggressive);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            pick = i;
                        }
                    }

                    if (pick < 0)
                        break;

                    var chosen = hand[pick];
                    mana -= Mathf.Max(0, chosen.Cost + handOffsets[pick]);

                    if (chosen.Damage > 0)
                    {
                        var damage = Mathf.RoundToInt(chosen.Damage * (1f + noise / 10f * 0.03f));
                        totalDamage += damage;
                        var absorbedByEnemy = Mathf.Min(enemyShield, damage);
                        enemyShield -= absorbedByEnemy;
                        enemyHp = Mathf.Max(0, enemyHp - (damage - absorbedByEnemy));
                    }

                    if (chosen.Shield > 0)
                        playerShield += chosen.Shield;

                    noise = Mathf.Clamp(noise + chosen.Noise, 0, 100);
                    mana += chosen.Mana;
                    played++;

                    hand.RemoveAt(pick);
                    handOffsets.RemoveAt(pick);
                    discard.Add(chosen);
                }

                if (played == 0)
                    lockedTurns++;

                overflowMana += Mathf.Max(0, mana - manaCap);

                // 玩家回合结束：先结算崩溃反噬与噪声衰减，再进入敌人回合（顺序与游戏一致）。
                var tierBefore = TierFor(noise);
                if (tierBefore == EchoNoiseTier.Collapse)
                    playerHp = Mathf.Max(0, playerHp - EchoDistortion.CollapseBacklash);

                noise = Mathf.Clamp(noise - 5, 0, 100);
                if (tierBefore == EchoNoiseTier.Collapse)
                    noise = Mathf.Clamp(noise - 8, 0, 100);
                else if (tierBefore == EchoNoiseTier.Drift)
                    noise = Mathf.Clamp(noise - 2, 0, 100);

                // 敌人回合：按三张牌循环行动。
                var incoming = 0;
                if (enemyStep == 0)
                {
                    incoming = Mathf.RoundToInt(9 * 0.75f);
                }
                else if (enemyStep == 1)
                {
                    enemyShield += 6;
                    noise = Mathf.Clamp(noise + 5, 0, 100);
                }
                else
                {
                    incoming = 4;
                }

                if (incoming > 0)
                {
                    var absorbed = Mathf.Min(playerShield, incoming);
                    playerShield -= absorbed;
                    playerHp = Mathf.Max(0, playerHp - (incoming - absorbed));
                }
                enemyStep = (enemyStep + 1) % 3;

                report.AppendLine(string.Format(
                    "  T{0,2}  法力 {1}  出牌 {2}  噪声 {3,3}  {4}  HP {5}  敌HP {6}",
                    turn, manaCap, played, noise, TierName(TierFor(noise)), playerHp, enemyHp));

                if (enemyHp <= 0)
                {
                    killTurn = turn;
                    break;
                }

                if (playerHp <= 0)
                {
                    deathTurn = turn;
                    break;
                }
            }

            report.AppendLine(string.Format(
                "  合计：伤害 {0}  失真回合 {1}  崩溃回合 {2}  溢出法力 {3}  空过回合 {4}  玩家 HP {5}  {6}",
                totalDamage, distortedTurns, chaosTurns, overflowMana, lockedTurns, playerHp,
                killTurn > 0 ? "T" + killTurn + " 击杀" : deathTurn > 0 ? "T" + deathTurn + " 阵亡" : "12 回合未分胜负"));
        }

        /// <summary>返回 0 表示这回合不打这张牌。</summary>
        private static int Score(SimCard card, int noise, bool aggressive)
        {
            if (card.Mana > 0)
            {
                // 蓄能牌：全力输出会一直用（哪怕噪声爆表），攻守平衡只在安全时用。
                if (aggressive)
                    return noise < 85 ? 120 : 40;
                return noise < 36 ? 90 : 0;
            }

            if (card.Damage > 0)
                return aggressive ? 100 : 80;

            if (card.Shield > 0)
            {
                // 全力输出也会在法力富余时补盾，否则手牌会被防御牌塞满。
                if (aggressive)
                    return 30;
                return noise >= 36 ? 110 : 60;
            }

            return 0;
        }

        private static void Draw(
            List<SimCard> drawPile,
            List<SimCard> discard,
            List<SimCard> hand,
            List<int> offsets,
            int count,
            System.Random random,
            EchoNoiseTier tier)
        {
            while (hand.Count < count)
            {
                if (drawPile.Count == 0)
                {
                    if (discard.Count == 0)
                        return;
                    drawPile.AddRange(discard);
                    discard.Clear();
                }

                var index = random.Next(drawPile.Count);
                var card = drawPile[index];
                drawPile.RemoveAt(index);
                hand.Add(card);
                offsets.Add(EchoDistortion.RollCostOffset(card.Cost, tier));
            }
        }

        private static EchoNoiseTier TierFor(int noise)
        {
            var config = EchoConfig.Instance;
            return config != null ? config.TierFor(noise) : EchoNoiseTier.Stable;
        }

        private static string TierName(EchoNoiseTier tier)
        {
            switch (tier)
            {
                case EchoNoiseTier.Drift:
                    return "漂移";
                case EchoNoiseTier.Collapse:
                    return "崩溃";
                default:
                    return "稳定";
            }
        }
    }
}

