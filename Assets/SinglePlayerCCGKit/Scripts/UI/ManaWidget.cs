// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using TMPro;
using UnityEngine;

namespace CCGKit
{
    /// <summary>
    /// The widget used to display the player's mana.
    /// </summary>
    public class ManaWidget : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        private TextMeshProUGUI text;
        [SerializeField]
        private TextMeshProUGUI textBorder;
#pragma warning restore 649

        public void Initialize(IntVariable mana)
        {
            SetValue(mana.Value);
        }

        private void SetValue(int value)
        {
            // === Variables Echo integration ===
            // 崩溃档噪声会让每回合的法力上限在 2/4 之间跳动，分母不再有意义，只显示当前值。
            text.text = value.ToString();
            textBorder.text = text.text;
            // === End Variables Echo integration ===
        }

        public void OnManaChanged(int value)
        {
            SetValue(value);
        }
    }
}
