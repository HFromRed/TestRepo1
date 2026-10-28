// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store EULA,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CCGKit
{
    /// <summary>
    /// The "Cards" tab in the editor.
    /// </summary>
    public class CardsTab : EditorTab
    {
        private CardTemplate currentCard;

        private ReorderableList effectsList;
        private Effect currentEffect;

        public CardsTab(SinglePlayerCcgKitEditor editor) :
            base(editor)
        {
        }

        public override void Draw()
        {
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 50;

            GUILayout.Space(15);

            var prevCard = currentCard;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);
                currentCard = (CardTemplate)EditorGUILayout.ObjectField(
                    "Asset", currentCard, typeof(CardTemplate), false, GUILayout.Width(340));
            }
            GUILayout.EndHorizontal();

            if (currentCard != prevCard)
            {
                if (currentCard != null)
                {
                    CreateEffectsList();
                    currentEffect = null;
                }
            }

            if (currentCard != null)
            {
                DrawCurrentCard();

                if (GUI.changed)
                    EditorUtility.SetDirty(currentCard);
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private void DrawCurrentCard()
        {
            GUILayout.BeginVertical();
            {
                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(10);

                    GUILayout.BeginVertical("GroupBox", GUILayout.Width(100));
                    {
                        GUILayout.BeginVertical();
                        {
                            GUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField(new GUIContent("Id", "The unique identifier of this card."),
                                    GUILayout.Width(EditorGUIUtility.labelWidth));
                                currentCard.Id = EditorGUILayout.IntField(currentCard.Id, GUILayout.Width(30));
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.Space(5);

                            GUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField(new GUIContent("Name", "The name of this card."),
                                    GUILayout.Width(EditorGUIUtility.labelWidth));
                                currentCard.Name = EditorGUILayout.TextField(currentCard.Name, GUILayout.Width(150));
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.Space(5);

                            GUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField(new GUIContent("Cost", "The cost of this card."),
                                    GUILayout.Width(EditorGUIUtility.labelWidth));
                                currentCard.Cost = EditorGUILayout.IntField(currentCard.Cost, GUILayout.Width(30));
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.Space(5);

                            GUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField(new GUIContent("Type", "The type of this card."),
                                    GUILayout.Width(EditorGUIUtility.labelWidth));
                                currentCard.Type = (CardType)EditorGUILayout.ObjectField(
                                    currentCard.Type, typeof(CardType), false, GUILayout.Width(200));
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.Space(5);

                            GUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField(new GUIContent("Material", "The material of this card."),
                                    GUILayout.Width(EditorGUIUtility.labelWidth));
                                currentCard.Material = (Material)EditorGUILayout.ObjectField(
                                    "", currentCard.Material, typeof(Material), false, GUILayout.Width(200));
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.Space(5);

                            GUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField(new GUIContent("Picture", "The picture of this card."),
                                    GUILayout.Width(EditorGUIUtility.labelWidth));
                                currentCard.Picture = (Sprite)EditorGUILayout.ObjectField(
                                    "", currentCard.Picture, typeof(Sprite), false, GUILayout.Width(70));
                            }
                            GUILayout.EndHorizontal();

                            GUILayout.Space(5);

                            GUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.LabelField(new GUIContent("Upgrade", "The card this one upgrades to."),
                                    GUILayout.Width(EditorGUIUtility.labelWidth));
                                currentCard.Upgrade = (CardTemplate)EditorGUILayout.ObjectField(
                                    "", currentCard.Upgrade, typeof(CardTemplate), false, GUILayout.Width(200));
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(10);

                    GUILayout.BeginVertical(GUILayout.Width(300));
                    {
                        effectsList?.DoLayoutList();
                    }
                    GUILayout.EndVertical();

                    if (effectsList != null)
                        DrawCurrentEffect();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void DrawCurrentEffect()
        {
            if (currentEffect != null)
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(17);

                    GUILayout.BeginHorizontal();
                    {
                        currentEffect.CreateSourceActionsList();
                        currentEffect.CreateTargetActionsList();
                        currentEffect.Draw();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
        }

        private void CreateEffectsList()
        {
            effectsList = SetupReorderableList("Effects", currentCard.Effects, (rect, x) =>
                {
                },
                x =>
                {
                    currentEffect = x;
                },
                () =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(
                        new GUIContent("Deal damage"), false, CreateEffectCallback, typeof(DealDamageEffect));
                    menu.AddItem(
                        new GUIContent("Gain mana"), false, CreateEffectCallback, typeof(GainManaEffect));
                    menu.AddItem(
                        new GUIContent("Gain HP"), false, CreateEffectCallback, typeof(GainHpEffect));
                    menu.AddItem(
                        new GUIContent("Gain shield"), false, CreateEffectCallback, typeof(GainShieldEffect));
                    menu.AddItem(
                        new GUIContent("Apply buff"), false, CreateEffectCallback, typeof(ApplyStatusEffect));
                    menu.AddItem(
                        new GUIContent("Draw cards"), false, CreateEffectCallback, typeof(DrawCardsEffect));
                    // === Variables Echo integration ===
                    menu.AddItem(
                        new GUIContent("Variables Echo/Noise"), false, CreateEffectCallback, typeof(VariablesEcho.NoiseEffect));
                    menu.AddItem(
                        new GUIContent("Variables Echo/Reflect"), false, CreateEffectCallback, typeof(VariablesEcho.ReflectEffect));
                    menu.AddItem(
                        new GUIContent("Variables Echo/Predict Lock"), false, CreateEffectCallback, typeof(VariablesEcho.PredictLockEffect));
                    menu.AddItem(
                        new GUIContent("Variables Echo/Self Damage"), false, CreateEffectCallback, typeof(VariablesEcho.SelfDamageEffect));
                    // === End Variables Echo integration ===
                    menu.ShowAsContext();
                },
                x =>
                {
                    UnityEngine.Object.DestroyImmediate(currentEffect, true);
                    currentEffect = null;
                });
                
            effectsList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = currentCard.Effects[index];
                
                rect.y += 2;
                rect.width -= 10;
                rect.height = EditorGUIUtility.singleLineHeight;

                var label = element.GetName();
                EditorGUI.LabelField(rect, label, EditorStyles.boldLabel);
                rect.y += 5;
                rect.y += EditorGUIUtility.singleLineHeight;

                element.Draw(rect);
            };

            effectsList.elementHeightCallback = (index) =>
            {
                var element = currentCard.Effects[index];
                return element.GetHeight();
            };
        }

        private void CreateEffectCallback(object obj)
        {
            var effect = ScriptableObject.CreateInstance((Type)obj) as Effect;
            if (effect != null)
            {
                effect.hideFlags = HideFlags.HideInHierarchy;
                currentCard.Effects.Add(effect);
                AssetDatabase.AddObjectToAsset(effect, currentCard);
            }
        }
    }
}
