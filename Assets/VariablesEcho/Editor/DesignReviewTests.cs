using System;
using UnityEditor;
using UnityEngine;
using VariablesEcho;
namespace VariablesEchoEditor
{
    public static class DesignReviewTests
    {
        public static void Run()
        {
            var icon=Sprite.Create(new Texture2D(2,2),new Rect(0,0,2,2),Vector2.zero);
            for(int value=1;value<=100;value++)
            {
                Sprite actual;string text;
                EchoDistortion.DistortIntent(EchoNoiseTier.Drift,icon,value,out actual,out text);
                var bounds=text.Split('~');int lo=int.Parse(bounds[0]),hi=int.Parse(bounds[1]);
                if(value<lo||value>hi||hi-lo!=4)throw new Exception("Invalid threat envelope");
                for(int other=lo;other<=hi;other++)
                {
                    string second;EchoDistortion.DistortIntent(EchoNoiseTier.Drift,icon,other,out actual,out second);
                    if(second!=text)throw new Exception("Envelope reveals exact magnitude");
                }
                if(EchoDistortion.RollCostOffset(value,EchoNoiseTier.Collapse)!=0||EchoDistortion.RollMana(3,EchoNoiseTier.Collapse)!=3)throw new Exception("Unstable economy");
            }
            EchoDistortion.RevealThisTurn();
            Sprite revealed;string exact;
            EchoDistortion.DistortIntent(EchoNoiseTier.Collapse,icon,17,out revealed,out exact);
            if(exact!="17"||revealed!=icon)throw new Exception("Calibration failed to restore real intent");
            EchoDistortion.OnPlayerTurnBegan();
            UnityEngine.Object.DestroyImmediate(icon.texture);UnityEngine.Object.DestroyImmediate(icon);
            Debug.Log("DESIGN_REVIEW_TEST_PASS: 100 magnitudes, shared envelopes, stable economy, collapse reveal");
            EchoDistortionSelfTest.Run();ChapterBuildPlayer.Build();
        }
    }
}
