// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;
using System.Collections.Generic;
using UnityEngine;
using VariablesEcho;

namespace CCGKit
{
    [Serializable]
    [CreateAssetMenu(
        menuName = "Single-Player CCG Kit/Templates/Card",
        fileName = "Card",
        order = 0)]
    public class CardTemplate : ScriptableObject
    {
        public int Id;
        public string Name;
        public int Cost;
        public Material Material;
        public Sprite Picture;
        public CardType Type;
        public CardTemplate Upgrade;
        public List<Effect> Effects = new List<Effect>();

        // === Variables Echo integration ===
        [Header("Variables Echo / 变量残响")]
        [Tooltip("稳定 ID。建议通过菜单 Variables Echo/Fill Card IDs 自动填充。")]
        public string CardId;
        public EchoCardFaction Faction = EchoCardFaction.Logic;
        public EchoCardRarity Rarity = EchoCardRarity.Common;
        [Tooltip("打出时的噪声变化（正数升噪，负数降噪）。")]
        public int NoiseDelta;
        [Tooltip("自伤/自毁牌，用于行为标签。")]
        public bool IsSelfHarm;
        [Tooltip("服从类永久牌：不会进入弃牌堆循环，且不可被移除。")]
        public bool IsPermanent;

        public bool IsNoiseCard
        {
            get { return Faction == EchoCardFaction.Noise || NoiseDelta > 0; }
        }
        // === End Variables Echo integration ===
    }
}
