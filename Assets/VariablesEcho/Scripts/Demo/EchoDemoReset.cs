using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 战斗演示场景专用：每次 Play 前清除 Kit 的临时战斗存档，
    /// 保证演示总是从 Warden 初始卡组开始，而不是上一次的地图存档。
    /// 正式游戏不要挂这个组件。
    /// </summary>
    public class EchoDemoReset : MonoBehaviour
    {
        [SerializeField]
        private bool resetKitProgressOnPlay = true;

        private void Awake()
        {
            if (resetKitProgressOnPlay)
            {
                PlayerPrefs.DeleteKey("save");
                PlayerPrefs.DeleteKey("map");
                PlayerPrefs.Save();
            }

            // 背景图缩放修正：Kit 场景里的背景对象是按原素材尺寸调的，
            // 换成演示背景后需要重新铺满相机视野。
            if (GetComponent<EchoBattleStageFix>() == null)
                gameObject.AddComponent<EchoBattleStageFix>();
        }
    }
}
