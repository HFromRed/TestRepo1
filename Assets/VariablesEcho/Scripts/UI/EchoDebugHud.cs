using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VariablesEcho
{
    /// <summary>
    /// 像素风噪声显示屏。三段像素仪表（稳定 / 漂移 / 崩溃）依次点亮，
    /// 只靠颜色与填充量表达噪声多少，默认不显示任何文字。
    /// 按 F1 隐藏/显示，按 F2 显示调试文本（行为标签、意图明细）。
    /// </summary>
    public sealed class EchoDebugHud : MonoBehaviour
    {
        private const int Rows = 3;
        private const int CellsPerRow = 12;
        private const float CellWidth = 10f;
        private const float CellHeight = 18f;
        private const float CellGap = 3f;
        private const float RowGap = 52f;
        private const float CellsLeft = 22f;

        private static readonly Color PanelFill = new Color(0.04f, 0.06f, 0.12f, 0.94f);
        private static readonly Color FrameColor = new Color(0.24f, 0.72f, 0.88f, 0.85f);
        private static readonly Color EmptyCell = new Color(0.09f, 0.13f, 0.22f, 0.95f);
        private static readonly Color StableColor = new Color(0.24f, 0.90f, 1.00f, 1f);
        private static readonly Color DriftColor = new Color(1.00f, 0.74f, 0.20f, 1f);
        private static readonly Color CollapseColor = new Color(1.00f, 0.26f, 0.32f, 1f);

        private GameObject panel;
        private Image frameImage;
        private Image[,] cells;
        private TextMeshProUGUI debugText;
        private bool visible = true;
        private bool showDebug;

        public static EchoDebugHud CreateIfNeeded()
        {
            var existing = UnityEngine.Object.FindObjectOfType<EchoDebugHud>();
            if (existing != null)
                return existing;

            var root = new GameObject("[EchoNoiseDisplay]");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var hud = root.AddComponent<EchoDebugHud>();
            hud.Build(root.transform);
            return hud;
        }

        private void Build(Transform canvasTransform)
        {
            const float width = 190f;
            const float height = 208f;

            var shadow = CreateImage(canvasTransform, "Shadow", EchoPixelUI.Solid);
            shadow.color = new Color(0f, 0f, 0f, 0.45f);
            Place(shadow.rectTransform, 18f, -18f, width, height);

            panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(canvasTransform, false);

            var frameImage = panel.GetComponent<Image>();
            frameImage.sprite = EchoPixelUI.Frame;
            frameImage.type = Image.Type.Sliced;
            frameImage.color = FrameColor;
            frameImage.raycastTarget = false;
            Place(panel.GetComponent<RectTransform>(), 14f, -14f, width, height);
            this.frameImage = frameImage;

            var inner = CreateImage(panel.transform, "Fill", EchoPixelUI.Solid);
            inner.color = PanelFill;
            var innerRect = inner.rectTransform;
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(7f, 7f);
            innerRect.offsetMax = new Vector2(-7f, -7f);

            cells = new Image[Rows, CellsPerRow];
            for (var row = 0; row < Rows; row++)
            {
                // 左侧色标：标出三段各自的档位颜色，无需文字也能看懂
                var legend = CreateImage(panel.transform, "Legend" + row, EchoPixelUI.Solid);
                legend.color = (row == 0 ? StableColor : row == 1 ? DriftColor : CollapseColor) * 0.75f;
                Place(legend.rectTransform, 10f, -34f - row * RowGap, 6f, CellHeight);

                for (var col = 0; col < CellsPerRow; col++)
                {
                    var cell = CreateImage(panel.transform, "Cell" + row + "_" + col, EchoPixelUI.Cell);
                    cell.color = EmptyCell;
                    Place(cell.rectTransform,
                        CellsLeft + col * (CellWidth + CellGap),
                        -30f - row * RowGap,
                        CellWidth,
                        CellHeight);
                    cells[row, col] = cell;
                }
            }

            debugText = CreateDebugText(canvasTransform);
        }

        private static TextMeshProUGUI CreateDebugText(Transform canvasTransform)
        {
            var go = new GameObject("DebugText", typeof(RectTransform));
            go.transform.SetParent(canvasTransform, false);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 15f;
            label.color = new Color(0.72f, 0.82f, 0.95f, 1f);
            label.alignment = TextAlignmentOptions.BottomLeft;
            label.raycastTarget = false;

            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(18f, 18f);
            rect.sizeDelta = new Vector2(760f, 150f);
            return label;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>以左上角为锚点、按屏幕像素摆放。</summary>
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F1))
                visible = !visible;
            if (Input.GetKeyDown(KeyCode.F2))
                showDebug = !showDebug;
#endif

            var battle = EchoRuntime.Battle;
            if (battle == null)
            {
                if (panel != null)
                    panel.SetActive(false);
                if (debugText != null)
                    debugText.enabled = false;
                return;
            }

            if (panel != null)
                panel.SetActive(visible);

            var noise = battle.Noise;
            var config = noise.Config;
            var value = noise.Value;

            // 三段各自覆盖一段区间：0~稳定上限 / 稳定~漂移上限 / 漂移~满值
            var ratios = new[]
            {
                Mathf.Clamp01(value / Mathf.Max(1f, config.stableMax)),
                Mathf.Clamp01((value - config.stableMax) /
                              Mathf.Max(1f, config.driftMax - config.stableMax)),
                Mathf.Clamp01((value - config.driftMax) /
                              Mathf.Max(1f, config.maxNoise - config.driftMax))
            };

            var tier = noise.Tier;
            var tierIndex = Mathf.Clamp((int)tier, 0, Rows - 1);
            var pulse = tier == EchoNoiseTier.Collapse
                ? Mathf.Lerp(0.55f, 1f, Mathf.PingPong(Time.unscaledTime * 3.2f, 1f))
                : 1f;

            // 边框颜色跟随档位：青 → 橙 → 红，崩溃档整体闪烁表示"系统失稳"。
            if (frameImage != null)
            {
                if (tier == EchoNoiseTier.Stable)
                    frameImage.color = FrameColor;
                else if (tier == EchoNoiseTier.Drift)
                    frameImage.color = new Color(1f, 0.74f, 0.20f, 0.85f);
                else
                    frameImage.color = new Color(1f, 0.30f, 0.36f, Mathf.Lerp(0.45f, 0.95f, pulse));
            }

            for (var row = 0; row < Rows; row++)
            {
                var rowColor = row == 0 ? StableColor : row == 1 ? DriftColor : CollapseColor;
                var lit = Mathf.RoundToInt(ratios[row] * CellsPerRow);

                for (var col = 0; col < CellsPerRow; col++)
                {
                    var on = col < lit;
                    var color = on ? rowColor : EmptyCell;

                    if (on && row == tierIndex)
                    {
                        color.a = pulse;
                        // 当前档位的最后一个亮点额外提亮，形成“正在读数”的感觉
                        if (col == lit - 1)
                            color = Color.Lerp(color, Color.white, 0.35f);
                    }
                    else if (on && row > tierIndex)
                    {
                        color.a = 0.35f;
                    }

                    cells[row, col].color = color;
                }
            }

            if (debugText != null)
            {
                debugText.enabled = showDebug;
                if (showDebug)
                    debugText.text = BuildDebugText(battle, noise);
            }
        }

        private static string BuildDebugText(EchoBattleContext battle, NoiseController noise)
        {
            var builder = new StringBuilder();
            builder.AppendFormat("noise {0}   {1}   {2}\n",
                noise.Value,
                noise.Tier.ToString().ToUpperInvariant(),
                noise.IsTracked ? "TRACKED" : "UNMARKED");
            builder.AppendFormat("behavior {0}   conf {1:0.00}   streak {2}\n",
                battle.Tagger.Evaluate().ToString().ToUpperInvariant(),
                noise.Confidence,
                noise.HighNoiseStreak);

            if (battle.Intents != null)
            {
                for (var i = 0; i < battle.Intents.Length; i++)
                {
                    if (battle.Intents[i] != null)
                        builder.AppendLine(battle.Intents[i].Describe());
                }
            }

            return builder.ToString();
        }

        private void OnDestroy()
        {
            panel = null;
            cells = null;
            debugText = null;
        }
    }
}
