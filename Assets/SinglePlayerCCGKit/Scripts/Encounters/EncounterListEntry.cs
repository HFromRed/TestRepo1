// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CCGKit
{
    [Serializable]
    public class EncounterListEntry
    {
        public string Name;
        public Sprite Background;
        public List<AssetReference> Enemies = new List<AssetReference>();
    }
}