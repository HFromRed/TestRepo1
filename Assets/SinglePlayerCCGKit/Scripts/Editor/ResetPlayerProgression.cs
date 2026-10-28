// Copyright (C) 2019-2021 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEditor;
using UnityEngine;

namespace GameVanilla.Editor
{
    /// <summary>
    /// Utility class for deleting the PlayerPrefs from within the editor.
    /// </summary>
    public class ResetPlayerProgression
    {
        [MenuItem("Tools/Single-Player CCG Kit/Reset player progression", false, 2)]
        public static void DeleteAllPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}
