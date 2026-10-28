// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CCGKit
{
    public class CardWidget : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        private TextMeshProUGUI costText;
        [SerializeField]
        private TextMeshProUGUI nameText;
        [SerializeField]
        private TextMeshProUGUI typeText;
        [SerializeField]
        private TextMeshProUGUI descriptionText;

        [SerializeField]
        private Image picture;
#pragma warning restore 649

        public CardTemplate Card;

        public void SetInfo(RuntimeCard card)
        {
            SetInfo(card.Template);
        }

        public void SetInfo(CardTemplate template)
        {
            Card = template;

            costText.text = template.Cost.ToString();
            nameText.text = template.Name;
            typeText.text = "Spell";
            var builder = new StringBuilder();
            foreach (var effect in template.Effects)
            {
                builder.AppendFormat("{0}. ", effect.GetName());
            }
            descriptionText.text = builder.ToString();
            picture.sprite = template.Picture;
        }
    }
}