using CCGKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VariablesEcho
{
    /// <summary>
    /// 伤害飘字。订阅 EchoEvents 的伤害事件，在受击角色上方弹出数值，
    /// 让每次攻击都有明确反馈（Kit 本身没有伤害数字）。
    /// </summary>
    public sealed class EchoFloatingText : MonoBehaviour
    {
        private static readonly Color DamageColor = new Color(1f, 0.55f, 0.25f, 1f);
        private static readonly Color PlayerHurtColor = new Color(1f, 0.32f, 0.36f, 1f);
        private static readonly Color ReflectColor = new Color(0.38f, 0.92f, 1f, 1f);

        private Canvas canvas;

        public static void Install()
        {
            if (Object.FindObjectOfType<EchoFloatingText>() != null)
                return;

            var root = new GameObject("[EchoFloatingText]");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 620;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var floating = root.AddComponent<EchoFloatingText>();
            floating.canvas = canvas;
        }

        private void OnEnable()
        {
            EchoEvents.DamageDealt += OnDamageDealt;
            EchoEvents.Reflected += OnReflected;
        }

        private void OnDisable()
        {
            EchoEvents.DamageDealt -= OnDamageDealt;
            EchoEvents.Reflected -= OnReflected;
        }

        private void OnDamageDealt(RuntimeCharacter instigator, RuntimeCharacter target, int damage)
        {
            if (damage <= 0 || EchoRuntime.Battle == null)
                return;

            CharacterObject character;
            if (!EchoRuntime.Battle.TryGetCharacter(target, out character))
                return;

            var isPlayer = EchoRuntime.Battle.IsPlayer(target);
            Spawn(character, "-" + damage, isPlayer ? PlayerHurtColor : DamageColor, isPlayer ? 1.15f : 1f);
        }

        private void OnReflected(RuntimeCharacter target, int damage)
        {
            if (damage <= 0 || EchoRuntime.Battle == null)
                return;

            CharacterObject character;
            if (!EchoRuntime.Battle.TryGetCharacter(target, out character))
                return;

            Spawn(character, "-" + damage, ReflectColor, 1f);
        }

        private void Spawn(CharacterObject character, string text, Color color, float scale)
        {
            if (character == null || Camera.main == null)
                return;

            var go = new GameObject("Float", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = 46f;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200f, 72f);

            var screen = Camera.main.WorldToScreenPoint(
                character.transform.position + new Vector3(0f, 2.6f, 0f));
            screen.x += Random.Range(-26f, 26f);

            // 屏幕坐标 → UGUI 本地坐标（屏幕空间画布，pivot 在中心）
            var canvasRect = (RectTransform)canvas.transform;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
            rect.anchoredPosition = local;
            rect.localScale = Vector3.one * scale;

            var motion = go.AddComponent<EchoFloatingMotion>();
            motion.target = rect;
            motion.colorTarget = label;
            motion.scale = scale;
        }
    }

    /// <summary>飘字的上浮与淡出。</summary>
    public sealed class EchoFloatingMotion : MonoBehaviour
    {
        public RectTransform target;
        public Graphic colorTarget;
        public float scale = 1f;

        private const float Duration = 0.85f;

        private float age;
        private Vector2 start;
        private Color baseColor;

        private void Start()
        {
            start = target.anchoredPosition;
            baseColor = colorTarget.color;
        }

        private void Update()
        {
            age += Time.deltaTime;
            var t = Mathf.Clamp01(age / Duration);

            target.anchoredPosition = start + new Vector2(0f, 70f * Mathf.SmoothStep(0f, 1f, t));

            var color = baseColor;
            color.a = 1f - t * t;
            colorTarget.color = color;

            // 起手放大再回落，制造“数字弹出”的冲击感
            var pop = Mathf.Lerp(1.35f, 1f, Mathf.Min(1f, t * 4f));
            target.localScale = Vector3.one * (scale * pop);

            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
