// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using DG.Tweening;
using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using VariablesEcho;

namespace CCGKit
{
    /// <summary>
    /// This component is linked to the actual GameObjects corresponding to the cards that
    /// are in the player's hand.
    /// </summary>
    public class CardObject : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        private TextMeshPro costText;
        [SerializeField]
        private TextMeshPro nameText;
        [SerializeField]
        private TextMeshPro typeText;
        [SerializeField]
        private TextMeshPro descriptionText;

        [SerializeField]
        private SpriteRenderer picture;
        [SerializeField]
        private SpriteRenderer glow;

        [SerializeField] 
        private Color inHandColor;
        [SerializeField] 
        private Color aboutToBePlayedColor;
#pragma warning restore 649

        public RuntimeCard RuntimeCard;
        public CardTemplate Template;

        private SortingGroup sortingGroup;

        private Vector3 cachedPos;
        private Quaternion cachedRot;
        private int cachedSortingOrder;
        private int highlightedSortingOrder;

        private bool interactable;

        private HandPresentationSystem handPresentationSystem;
        private CardWithArrowSelectionSystem cardWithArrowSelectionSystem;
        private CardWithoutArrowSelectionSystem cardWithoutArrowSelectionSystem;

        private bool beingHighlighted;
        private bool beingUnhighlighted;

        // === Variables Echo integration ===
        /// <summary>崩溃档噪声导致的费用偏移，这张牌进入手牌时掷一次。</summary>
        private int costOffset;
        private Color baseCostColor;
        private bool hasBaseCostColor;

        public int CostOffset
        {
            get { return costOffset; }
        }

        /// <summary>实际需要支付的法力。</summary>
        public int EffectiveCost
        {
            get { return Mathf.Max(0, Template.Cost + costOffset); }
        }
        // === End Variables Echo integration ===

        public enum CardState
        {
            InHand,
            AboutToBePlayed
        }

        public CardState State => currState;
        private CardState currState;

        private void Awake()
        {
            sortingGroup = GetComponent<SortingGroup>();
            SetGlowEnabled(false);
            // === Variables Echo integration ===
            baseCostColor = costText.color;
            hasBaseCostColor = true;
            // === End Variables Echo integration ===
        }

        private void Start()
        {
            handPresentationSystem = FindObjectOfType<HandPresentationSystem>();
            cardWithArrowSelectionSystem = FindObjectOfType<CardWithArrowSelectionSystem>();
            cardWithoutArrowSelectionSystem = FindObjectOfType<CardWithoutArrowSelectionSystem>();
        }

        private void OnEnable()
        {
            SetState(CardState.InHand);
        }

        public void SetInfo(RuntimeCard card)
        {
            RuntimeCard = card;
            Template = card.Template;
            // === Variables Echo integration ===
            // 崩溃档噪声：每张牌进手时掷一次费用偏移，并直接把显示改成实际费用。
            costOffset = EchoDistortion.RollCostOffset(Template.Cost);
            costText.text = EffectiveCost.ToString();
            if (hasBaseCostColor)
            {
                costText.color = costOffset > 0
                    ? new Color(1f, 0.38f, 0.42f, baseCostColor.a)
                    : costOffset < 0
                        ? new Color(0.42f, 0.92f, 1f, baseCostColor.a)
                        : baseCostColor;
            }
            // === End Variables Echo integration ===
            nameText.text = Template.Name;
            typeText.text = Template.IsNoiseCard ? "OVERLOAD" : "PROTOCOL";
            var builder = new StringBuilder();
            foreach (var effect in Template.Effects)
            {
                builder.AppendFormat("{0}. ", effect.GetName());
            }
            descriptionText.text = builder.ToString();
            picture.material = Template.Material;
            picture.sprite = Template.Picture;
            // === Variables Echo integration: Ivory / Void art skin ===
            EchoArtSkin.ApplyCard(this, picture);
        }

        public void SetGlowEnabled(bool glowEnabled)
        {
            glow.enabled = glowEnabled;
        }

        public void SetGlowEnabled(int playerMana)
        {
            // === Variables Echo integration ===
            glow.enabled = playerMana >= EffectiveCost;
            // === End Variables Echo integration ===
        }

        public bool IsGlowEnabled()
        {
            return glow.enabled;
        }

        public void OnManaChanged(int mana)
        {
            // === Variables Echo integration ===
            SetGlowEnabled(EffectiveCost <= mana);
            // === End Variables Echo integration ===
        }

        public void SetState(CardState state)
        {
            currState = state;
            switch (currState)
            {
                case CardState.InHand:
                    glow.color = inHandColor;
                    break;
                
                case CardState.AboutToBePlayed:
                    glow.color = aboutToBePlayedColor;
                    break;
            }
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
        }

        public void CacheTransform(Vector3 position, Quaternion rotation)
        {
            cachedPos = position;
            cachedRot = rotation;
            cachedSortingOrder = sortingGroup.sortingOrder;
            highlightedSortingOrder = cachedSortingOrder + 10;
        }

        private void OnMouseEnter()
        {
            if (interactable)
            {
                HighlightCard();
                handPresentationSystem.UnHighlightOtherCards(gameObject);
            }
        }

        private void OnMouseExit()
        {
            if (interactable)
            {
                UnHighlightCard();
            }
        }

        public void HighlightCard()
        {
            if (cardWithArrowSelectionSystem.HasSelectedCard() ||
                cardWithoutArrowSelectionSystem.HasSelectedCard())
            {
                return;
            }

            if (beingHighlighted)
            {
                return;
            }

            beingHighlighted = true;

            sortingGroup.sortingOrder = highlightedSortingOrder;
            transform.DOMove(cachedPos + new Vector3(0.0f, 1.0f, 0.0f), 0.05f)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => beingHighlighted = false);
        } 

        public void UnHighlightCard()
        {
            if (cardWithArrowSelectionSystem.HasSelectedCard() ||
                cardWithoutArrowSelectionSystem.HasSelectedCard())
            {
                return;
            }

            if (beingUnhighlighted)
            {
                return;
            }

            beingUnhighlighted = true;

            sortingGroup.sortingOrder = cachedSortingOrder;
            var y = transform.position.y;
            var seq = DOTween.Sequence();
            transform.DOMove(cachedPos, 0.02f)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => beingUnhighlighted = false);
        }

        public void Reset(Action onComplete)
        {
            transform.DOMove(cachedPos, 0.2f);
            transform.DORotateQuaternion(cachedRot, 0.2f);
            sortingGroup.sortingOrder = cachedSortingOrder;
            onComplete();
        }
    }
}


