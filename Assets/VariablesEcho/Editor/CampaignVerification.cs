using UnityEditor;
using UnityEditor.SceneManagement;
namespace VariablesEchoEditor
{
    public static class CampaignVerification
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene(ChapterOneBuilder.Root+"/ChapterOne.unity");
            EditorApplication.isPlaying=true;
        }
    }
}
