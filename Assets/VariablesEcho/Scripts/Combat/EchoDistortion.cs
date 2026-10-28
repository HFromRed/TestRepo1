using System.Collections.Generic;
using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 噪声失真实装：噪声越高，敌人意图越模糊，崩溃档隐藏数值但保留行动类型；费用与能量保持稳定。
    ///
    /// 三档规则：
    ///   稳定（0~35）：意图精确（图标 + 真实数值），费用与法力正常。
    ///   漂移（36~69）：意图数值变成区间（如 7~11），图标仍可信；费用正常。
    ///   崩溃（70+） ：意图数值隐藏为“?”，保留行动类型；费用与基础能量保持稳定。
    ///
    /// 玩家的控制手段：打出防御牌降噪（每 5 点护盾 -2 噪声）；
    /// 噪声每回合按阵营自然衰减。
    /// </summary>
    public static class EchoDistortion
    {
        /// <summary>漂移档意图区间的浮动比例。</summary>
        public const float DriftSpread = 0.25f;

        /// <summary>崩溃档卡牌费用偏移的出现概率（-1 / 0 / +1）。</summary>
        private static readonly float[] CostOffsetChances = { 0.25f, 0.75f };

        /// <summary>崩溃档每回合结束的噪声反噬（穿透护盾）。</summary>
        public const int CollapseBacklash = 2;

        private static readonly List<IntentWidget> Widgets = new List<IntentWidget>();
        private static Sprite unknownSprite;

        /// <summary>本回合被“稳定协议”强制精确显示（预留给后续卡牌）。</summary>
        public static bool Revealed { get; private set; }

        public static EchoNoiseTier Tier
        {
            get
            {
                var battle = EchoRuntime.Battle;
                return battle == null ? EchoNoiseTier.Stable : battle.Noise.Tier;
            }
        }

        public static bool IsChaotic
        {
            get { return Tier == EchoNoiseTier.Collapse; }
        }

        public static void Install()
        {
            var battle = EchoRuntime.Battle;
            if (battle == null)
                return;

            // 噪声一变就重绘所有意图，避免“已经进入崩溃档但头顶还显示精确数字”。
            battle.Noise.Changed -= OnNoiseChanged;
            battle.Noise.Changed += OnNoiseChanged;
        }

        private static void OnNoiseChanged(NoiseController noise)
        {
            RefreshIntents();
        }

        public static void OnPlayerTurnBegan()
        {
            Revealed = false;
        }

        public static void RevealThisTurn()
        {
            Revealed = true;
            RefreshIntents();
        }

        public static void Register(IntentWidget widget)
        {
            if (widget != null && !Widgets.Contains(widget))
                Widgets.Add(widget);
        }

        public static void Unregister(IntentWidget widget)
        {
            Widgets.Remove(widget);
        }

        private static void RefreshIntents()
        {
            for (var i = 0; i < Widgets.Count; i++)
            {
                if (Widgets[i] != null)
                    Widgets[i].RefreshDistortion();
            }
        }

        /// <summary>
        /// 把真实意图映射为玩家看到的图标与文本。
        /// 返回 true 表示这次显示被噪声污染过。
        /// </summary>
        public static bool DistortIntent(Sprite source, int value, out Sprite sprite, out string text)
        {
            return DistortIntent(Tier, source, value, out sprite, out text);
        }

        /// <summary>指定档位的显示规则（自测与调试工具用）。</summary>
        public static bool DistortIntent(
            EchoNoiseTier tier, Sprite source, int value, out Sprite sprite, out string text)
        {
            sprite = source;
            text = value.ToString();

            if (Revealed || tier == EchoNoiseTier.Stable)
                return false;

            if (tier == EchoNoiseTier.Drift)
            {
                // Shared buckets prevent midpoint inversion of the true damage.
                var high = value <= 0 ? 0 : ((value - 1) / 5 + 1) * 5;
                var low = value <= 0 ? 0 : high - 4;
                text = string.Format("{0}~{1}", low, high);
                return true;
            }

            // Preserve action category: uncertainty concerns magnitude, not the ability to plan.
            text = "?";
            return true;
        }

        /// <summary>兼容 Kit 接口：所有档位费用偏移为 0。</summary>
        public static int RollCostOffset(int baseCost)
        {
            return RollCostOffset(baseCost, Tier);
        }

        public static int RollCostOffset(int baseCost, EchoNoiseTier tier)
        {
            // Noise changes information and exposes backlash; printed costs remain dependable.
            return 0;
        }

        public static int RollMana(int baseMana) { return baseMana; }
        public static int RollMana(int baseMana, EchoNoiseTier tier) { return baseMana; }
        /// <summary>崩溃档的意图占位图：像素问号 + 少量故障噪点。</summary>
        private static Sprite UnknownSprite
        {
            get
            {
                if (unknownSprite == null)
                    unknownSprite = BuildUnknownSprite();
                return unknownSprite;
            }
        }

        private static readonly string[] Glyph =
        {
            "01110",
            "10001",
            "00001",
            "00110",
            "00100",
            "00100",
            "00000",
            "00100",
            "00100"
        };

        private static Sprite BuildUnknownSprite()
        {
            const int scale = 6;
            const int glyphWidth = 5;
            const int glyphHeight = 9;
            var width = glyphWidth * scale;
            var height = glyphHeight * scale;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;

            var tint = new Color(0.55f, 0.95f, 1f, 1f);
            var glitch = new Color(1f, 0.35f, 0.6f, 1f);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var gx = x / scale;
                    var gy = glyphHeight - 1 - y / scale;
                    var on = Glyph[gy][gx] == '1';
                    var noise = (x * 31 + y * 17) % 23 == 0;
                    texture.SetPixel(x, y, on ? tint : noise ? glitch : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                90f,
                0,
                SpriteMeshType.FullRect);
        }
    }
}


