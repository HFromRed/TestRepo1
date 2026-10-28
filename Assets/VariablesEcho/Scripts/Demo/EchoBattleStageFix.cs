using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 战斗演示舞台修正：把 Background 层的背景图等比放大到完整覆盖相机视野（Cover），
    /// 并随 GameBootstrap 换图自动重算。挂在 BattleDemoGameInfo 上（由 EchoDemoReset 添加）。
    /// </summary>
    public class EchoBattleStageFix : MonoBehaviour
    {
        private SpriteRenderer background;
        private Sprite appliedSprite;

        private void LateUpdate()
        {
            if (background == null)
            {
                foreach (var renderer in FindObjectsOfType<SpriteRenderer>())
                {
                    if (renderer.sortingLayerName == "Background" && renderer.sprite != null)
                    {
                        background = renderer;
                        break;
                    }
                }

                if (background == null)
                    return;
            }

            if (background.sprite == appliedSprite)
                return;

            appliedSprite = background.sprite;
            Apply();
        }

        private void Apply()
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic || appliedSprite == null)
                return;

            // bounds.size 已含当前缩放，先除回去拿到未缩放的世界尺寸
            var size = appliedSprite.bounds.size;
            var viewH = cam.orthographicSize * 2f;
            var viewW = viewH * cam.aspect;
            var scale = Mathf.Max(viewW / size.x, viewH / size.y) * 1.02f;
            background.transform.localScale = new Vector3(scale, scale, 1f);

            var camPos = cam.transform.position;
            background.transform.position = new Vector3(camPos.x, camPos.y, background.transform.position.z);
        }
    }
}
