using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace VariablesEchoEditor
{
    public static class ChapterBuildPlayer
    {
        [MenuItem("Variables Echo/Chapter One/Build Windows Player")]
        public static void Build()
        {
            ChapterOneBuilder.Validate();
            Directory.CreateDirectory("Builds/ChapterOne");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{ChapterOneBuilder.Root+"/ChapterOne.unity",ChapterOneBuilder.Root+"/ChapterBattle.unity"},
                locationPathName="Builds/ChapterOne/VariablesEcho.exe",
                target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.Development
            });
            if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Player build failed: "+report.summary.result);
            Debug.Log("CHAPTER_PLAYER_BUILD_OK");
        }
    }
}
