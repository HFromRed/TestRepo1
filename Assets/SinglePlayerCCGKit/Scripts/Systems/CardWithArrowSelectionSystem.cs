// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace CCGKit
{
    /// <summary>
    /// This system creates a targeting arrow when the player selects a card from his hand that
    /// contains a targeted effect. This arrow allows the player to easily select the target of
    /// the card's effect.
    /// </summary>
    public class CardWithArrowSelectionSystem : CardSelectionSystem
    {
#pragma warning disable 649
        [SerializeField]
        private Canvas cardPileCanvas;
#pragma warning restore 649

        private RaycastHit2D[] raycastResults = new RaycastHit2D[1];
        private GameObject highlightedCard;

        private TargetingArrow targetingArrow;

        private GameObject selectedEnemy;

        private Vector3 prevClickPos;

        private bool isArrowCreated;

        private const float CardSelectionDetectionOffset = 2.2f;
        private const float CardSelectionAnimationTime = 0.2f;
        private const float CardSelectionCanceledAnimationTime = 0.2f;
        private const float SelectedCardYOffset = -4.0f;

        protected override void Start()
        {
            base.Start();
            targetingArrow = Object.FindObjectOfType<TargetingArrow>();
        }

        private void Update()
        {
            if (cardPileCanvas.isActiveAndEnabled)
                return;

            if (TurnManagementSystem.IsEndOfGame())
                return;

            if (HandPresentationSystem.IsAnimating())
                return;

            if (Input.GetMouseButtonDown(0))
            {
                DetectCardSelection();
                DetectEnemySelection();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                DetectEnemySelection();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                DetectCardUnselection();
            }

            if (SelectedCard != null)
                UpdateTargetingArrow();
        }

        private void DetectCardSelection()
        {
            // Checks if the player clicked over a card.
            var mousePos = MainCamera.ScreenToWorldPoint(Input.mousePosition);
            var hitInfo = Physics2D.Raycast(mousePos, Vector3.forward, Mathf.Infinity, CardLayer);
            if (hitInfo.collider != null)
            {
                var card = hitInfo.collider.GetComponent<CardObject>();
                var cardTemplate = card.Template;
                // === Variables Echo integration ===
                // 用含噪声偏移的实际费用判定，而不是卡牌模板的原始费用。
                if (CardUtils.CardCanBePlayed(card, PlayerMana) &&
                    CardUtils.CardHasTargetableEffect(cardTemplate))
                // === End Variables Echo integration ===
                {
                    SelectedCard = hitInfo.collider.gameObject;
                    SelectedCard.GetComponent<SortingGroup>().sortingOrder += 10;
                    prevClickPos = mousePos;
                }
            }
        }

        private void DetectCardUnselection()
        {
            if (SelectedCard != null)
            {
                var card = SelectedCard.GetComponent<CardObject>();
                SelectedCard.transform.DOKill();
                card.Reset(() =>
                {
                    SelectedCard = null;
                    isArrowCreated = false;
                });

                targetingArrow.EnableArrow(false);
            }
        }

        private void DetectEnemySelection()
        {
            if (SelectedCard != null)
            {
                // Checks if the player clicked over an enemy.
                var mousePos = MainCamera.ScreenToWorldPoint(Input.mousePosition);
                var hitInfo = Physics2D.Raycast(mousePos, Vector3.forward, Mathf.Infinity, EnemyLayer);
                if (hitInfo.collider != null)
                {
                    selectedEnemy = hitInfo.collider.gameObject;
                    PlaySelectedCard();

                    SelectedCard = null;

                    isArrowCreated = false;
                    targetingArrow.EnableArrow(false);
                }
            }
        }

        private void UpdateTargetingArrow()
        {
            var mousePos = MainCamera.ScreenToWorldPoint(Input.mousePosition);
            var diffY = mousePos.y - prevClickPos.y;
            if (!isArrowCreated && diffY > CardSelectionDetectionOffset)
            {
                isArrowCreated = true;

                var pos = SelectedCard.transform.position;

                SelectedCard.transform.DOKill();
                var seq = DOTween.Sequence();
                seq.AppendCallback(() =>
                    {
                        SelectedCard.transform.DOMove(new Vector3(0, SelectedCardYOffset, pos.z),
                            CardSelectionAnimationTime);
                        SelectedCard.transform.DORotate(Vector3.zero, CardSelectionAnimationTime);
                    });
                seq.AppendInterval(0.05f);
                seq.OnComplete(() => { targetingArrow.EnableArrow(true); });
            }
        }

        protected override void PlaySelectedCard()
        {
            base.PlaySelectedCard();

            var card = SelectedCard.GetComponent<CardObject>().RuntimeCard;
            EffectResolutionSystem.ResolveCardEffects(card, selectedEnemy.GetComponent<CharacterObject>());
        }

        public bool IsArrowActive()
        {
            return isArrowCreated;
        }

        public GameObject GetSelectedEnemy()
        {
            return selectedEnemy;
        }
    }
}
