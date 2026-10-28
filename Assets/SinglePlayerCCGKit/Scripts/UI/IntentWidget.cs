// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VariablesEcho;

namespace CCGKit
{
    /// <summary>
    /// The intent widget that displays the associated enemy's current intent.
    /// </summary>
    public class IntentWidget : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        private Image intentImage;
        [SerializeField]
        private TextMeshProUGUI amountText;
#pragma warning restore 649

        private bool isCharacterDead;

        // === Variables Echo integration ===
        // 记录真实意图，噪声档位变化时可以重新渲染为模糊/未知。
        private Sprite baseSprite;
        private int baseValue;
        private bool hasIntent;

        private void OnEnable()
        {
            EchoDistortion.Register(this);
        }

        private void OnDisable()
        {
            EchoDistortion.Unregister(this);
        }
        // === End Variables Echo integration ===

        private const float InitialDelay = 1.25f;
        private const float FadeInDuration = 0.8f;
        private const float FadeOutDuration = 0.5f;

        private void Start()
        {
            var transparentColor = intentImage.color;
            transparentColor.a = 0.0f;
            intentImage.color = transparentColor;
            amountText.color = transparentColor;
        }

        public void OnEnemyTurnBegan()
        {
            if (isCharacterDead)
            {
                return;
            }

            var seq = DOTween.Sequence();
            seq.AppendInterval(InitialDelay);
            seq.Append(intentImage.DOFade(0.0f, FadeOutDuration));

            seq = DOTween.Sequence();
            seq.AppendInterval(InitialDelay);
            seq.Append(amountText.DOFade(0.0f, FadeOutDuration));
        }

        public void OnIntentChanged(Sprite sprite, int value)
        {
            if (isCharacterDead)
            {
                return;
            }

            // === Variables Echo integration ===
            baseSprite = sprite;
            baseValue = value;
            hasIntent = sprite != null;
            ApplyDistortion();
            // === End Variables Echo integration ===

            intentImage.DOFade(1.0f, FadeInDuration);
            amountText.DOFade(1.0f, FadeInDuration);
        }

        // === Variables Echo integration ===
        /// <summary>噪声档位变化后重新渲染显示（真实意图仍然保存在 baseSprite/baseValue）。</summary>
        public void RefreshDistortion()
        {
            if (!hasIntent || isCharacterDead)
                return;
            ApplyDistortion();
        }

        private void ApplyDistortion()
        {
            Sprite sprite;
            string text;
            EchoDistortion.DistortIntent(baseSprite, baseValue, out sprite, out text);

            intentImage.sprite = sprite;
            intentImage.SetNativeSize();
            amountText.text = text;
        }
        // === End Variables Echo integration ===

        public void OnHpChanged(int hp)
        {
            if (hp <= 0)
            {
                isCharacterDead = true;
                intentImage.DOFade(0.0f, FadeOutDuration);
                amountText.DOFade(0.0f, FadeOutDuration);
            }
        }
    }
}
