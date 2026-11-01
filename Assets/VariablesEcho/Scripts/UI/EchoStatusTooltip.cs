using System.Collections.Generic;
using CCGKit;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VariablesEcho
{
    /// <summary>
    /// 状态图标悬停说明。Kit 的状态栏图标由 StatusWidget 在运行时实例化，
    /// 这里给它们挂上悬停组件，只有鼠标停留时才弹出像素风浮窗。
    /// </summary>
    public static class EchoStatusTooltip
    {
        private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
        {
            { "虚弱", "攻击伤害降低 25%，向下取整。自身回合结束时减少 1 层。" },
            { "力量", "每段攻击增加等于层数的伤害。自身回合结束时减少 1 层。" },
            { "增幅", "本场战斗每段攻击增加等于层数的伤害，回合结束不衰减。" },
            { "中毒", "回合开始时失去等于层数的生命，随后减少 1 层。" },
            { "易伤", "受到的攻击伤害增加 25%。自身回合结束时减少 1 层。" },
            { "反射", "受到正数攻击伤害时，对攻击者造成反伤，数值取反射层数与本次伤害的较小值。反伤直接扣除生命，护盾挡住攻击也能触发。本场战斗不自然衰减。" },
            { "预测锁定", "增加预测锁定层数，使下一次敌人意图显示更多信息。" },
            { "护盾", "优先抵挡攻击伤害。直接失去生命和反伤不受护盾保护。" },
            { "噪声", "提高输出并降低意图精度。0～35 显示精确意图；36～69 显示区间；70～100 隐藏数值，回合结束受到反噬。打出护盾牌可恢复本回合精确信息。" },
            { "抽牌", "从抽牌堆将卡牌加入手牌。抽牌堆耗尽时重新洗入弃牌。" },
            { "能量", "打出卡牌需要消耗对应能量，每回合按角色配置恢复。" },
            { "失去生命", "直接扣除生命，不经过护盾。" }
        };
        public static string Localize(string name)
        {
            switch(name) {
                case "Weak":return "虚弱";case "Strength":return "力量";case "Amplify":return "增幅";
                case "Poison":return "中毒";case "Fragile":return "易伤";
                case "Reflect":case "反伤":return "反射";case "PredictLock":return "预测锁定";
                default:return name;
            }
        }
        public static string Describe(string statusName)
        {
            if(string.IsNullOrEmpty(statusName))return string.Empty;
            string text;
            return Descriptions.TryGetValue(Localize(statusName),out text)?text:"由卡牌或敌人技能施加的状态，数值表示当前层数。";
        }
        public static string ExplainTerms(string text)
        {
            var parts=new List<string>();
            if(string.IsNullOrEmpty(text))return "";
            foreach(var pair in Descriptions)
                if(text.Contains(pair.Key) || (pair.Key=="抽牌" && text.Contains("抽取")) || (pair.Key=="反射" && text.Contains("反伤")))
                    parts.Add(pair.Key+"："+pair.Value);
            return string.Join("\n\n",parts);
        }

        public static void Install()
        {
            if (Object.FindObjectOfType<EchoStatusHoverHost>() != null)
                return;

            var root = new GameObject("[EchoStatusTooltip]");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 640;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            root.AddComponent<EchoStatusHoverHost>().Build(canvas, root.transform);
        }
    }

    /// <summary>扫描状态图标、挂悬停组件，并负责浮窗的显示与跟随。</summary>
    public sealed class EchoStatusHoverHost : MonoBehaviour
    {
        private static readonly Color FillColor = new Color(0.04f, 0.06f, 0.12f, 0.97f);
        private static readonly Color FrameColor = new Color(.87f,.84f,.75f,1);
        private static readonly Color TitleColor = new Color(.34f,.85f,.79f,1);
        private static readonly Color BodyColor = new Color(.87f,.84f,.75f,1);

        private const float PanelWidth = 340f;

        private Canvas canvas;
        private RectTransform tooltip;
        private RectTransform shadow;
        private TextMeshProUGUI title;
        private TextMeshProUGUI body;
        private float scanTimer;

        public void Build(Canvas rootCanvas, Transform root)
        {
            canvas = rootCanvas;

            shadow = CreateImage(root, "Shadow", EchoPixelUI.Solid).rectTransform;
            shadow.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            shadow.anchorMin = new Vector2(0f, 1f);
            shadow.anchorMax = new Vector2(0f, 1f);
            shadow.pivot = new Vector2(0f, 1f);

            tooltip = CreateImage(root, "Panel", EchoPixelUI.Frame).rectTransform;
            tooltip.GetComponent<Image>().type = Image.Type.Sliced;
            tooltip.GetComponent<Image>().color = FrameColor;
            tooltip.anchorMin = new Vector2(0f, 1f);
            tooltip.anchorMax = new Vector2(0f, 1f);
            tooltip.pivot = new Vector2(0f, 1f);

            var fill = CreateImage(tooltip, "Fill", EchoPixelUI.Solid);
            fill.color = FillColor;
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(7f, 7f);
            fill.rectTransform.offsetMax = new Vector2(-7f, -7f);

            var accent = CreateImage(tooltip, "Accent", EchoPixelUI.Solid);
            accent.color = new Color(0.40f, 0.86f, 1f, 0.55f);
            accent.rectTransform.anchorMin = new Vector2(0f, 1f);
            accent.rectTransform.anchorMax = new Vector2(0f, 1f);
            accent.rectTransform.pivot = new Vector2(0f, 1f);
            accent.rectTransform.anchoredPosition = new Vector2(16f, -14f);
            accent.rectTransform.sizeDelta = new Vector2(34f, 3f);

            title = CreateText(tooltip, "Title", 17f, FontStyles.Bold, TitleColor);
            PlaceTopLeft(title.rectTransform, 16f, -26f, PanelWidth - 32f, 24f);

            body = CreateText(tooltip, "Body", 14f, FontStyles.Normal, BodyColor);
            PlaceTopLeft(body.rectTransform, 16f, -56f, PanelWidth - 32f, 40f);

            shadow.gameObject.SetActive(false);
            tooltip.gameObject.SetActive(false);
            shadow.SetSiblingIndex(0);
        }

        private static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, string name, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.raycastTarget = false;
            return label;
        }

        private void Update()
        {
            scanTimer -= Time.unscaledDeltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = 0.25f;
                Scan();
            }

            if (tooltip != null && tooltip.gameObject.activeSelf)
                FollowMouse();
        }

        private void Scan()
        {
            foreach (var element in FindObjectsOfType<StatusElementWidget>())
            {
                if (element.GetComponent<EchoStatusHover>() == null)
                    element.gameObject.AddComponent<EchoStatusHover>().Host = this;
            }
        }

        public void Show(string statusName)
        {
            if (string.IsNullOrEmpty(statusName) || tooltip == null)
                return;

            title.text = EchoStatusTooltip.Localize(statusName);
            body.text = EchoStatusTooltip.Describe(statusName);

            body.ForceMeshUpdate();
            var bodyHeight = Mathf.Max(20f, body.preferredHeight);
            body.rectTransform.sizeDelta = new Vector2(PanelWidth - 32f, bodyHeight);

            var height = 56f + bodyHeight + 18f;
            tooltip.sizeDelta = new Vector2(PanelWidth, height);
            shadow.sizeDelta = tooltip.sizeDelta;

            tooltip.gameObject.SetActive(true);
            shadow.gameObject.SetActive(true);
            FollowMouse();
        }

        public void Hide()
        {
            if (tooltip == null)
                return;

            tooltip.gameObject.SetActive(false);
            shadow.gameObject.SetActive(false);
        }

        private void FollowMouse()
        {
            if (canvas == null)
                return;

            var scale = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            var size = tooltip.sizeDelta * scale;
            var mouse = (Vector2)Input.mousePosition;

            var x = mouse.x + 20f;
            var y = mouse.y + 26f;
            if (x + size.x > Screen.width)
                x = mouse.x - size.x - 20f;
            if (y + size.y > Screen.height)
                y = mouse.y - size.y - 10f;

            var canvasRect = (RectTransform)tooltip.parent;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, new Vector2(x, y), null, out local);

            // 锚点与轴心都设成左上角，因此要减去父矩形左上角的本地坐标
            var topLeft = new Vector2(-canvasRect.rect.width * 0.5f, canvasRect.rect.height * 0.5f);
            tooltip.anchoredPosition = local - topLeft;
            shadow.anchoredPosition = tooltip.anchoredPosition + new Vector2(5f, -5f);
        }
    }

    /// <summary>挂在状态图标上：悬停时请求浮窗，移开就收起。</summary>
    public sealed class EchoStatusHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public EchoStatusHoverHost Host;

        private StatusElementWidget element;

        private void Awake()
        {
            element = GetComponent<StatusElementWidget>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Host == null || element == null)
                return;
            Host.Show(element.Type);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (Host != null)
                Host.Hide();
        }

        private void OnDisable()
        {
            if (Host != null)
                Host.Hide();
        }
    }
}

