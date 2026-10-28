using CCGKit;
namespace VariablesEcho
{
    // Description only: NoiseDelta in EchoRuntime is the single source of the actual change.
    public class ChapterNoiseDescription : Effect
    {
        public int amount;
        public override string GetName() { return "Noise " + amount.ToString("+0;-0;0"); }
    }
}
