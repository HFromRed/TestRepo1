using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VariablesEcho
{
    // All scene and dynamically created text shares the bundled Chinese font.
    public sealed class EchoChineseFont : MonoBehaviour
    {
        static TMP_FontAsset shared;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var source = Resources.Load<Font>("Fonts/VonwaonBitmap-16px");
            if (source == null) { Debug.LogError("凤凰点阵字体资源缺失"); return; }
            shared = TMP_FontAsset.CreateFontAsset(source, 32, 4,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048,
                AtlasPopulationMode.Dynamic, true);
            shared.name = "凤凰点阵体";
            DontDestroyOnLoad(shared);
            var host = new GameObject("Chinese typography");
            DontDestroyOnLoad(host);
            host.AddComponent<EchoChineseFont>();
        }
        void LateUpdate()
        {
            foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (!text.gameObject.scene.IsValid() || text.font == shared) continue;
                text.font = shared;
            }
            foreach (var text in Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>())
                if (text.gameObject.scene.IsValid()) text.font = Resources.Load<Font>("Fonts/VonwaonBitmap-16px");
        }
    }
}
