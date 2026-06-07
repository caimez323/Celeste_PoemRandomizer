using Celeste.Mod;

namespace Celeste.Mod.PoemRandomizer {
    public class PoemRandomizerModule : EverestModule {
        public static PoemRandomizerModule Instance { get; private set; }

        public PoemRandomizerModule() {
            Instance = this;
        }

        public override void Load() {
            Logger.Log(LogLevel.Info, "PoemRandomizer", "Module Load appelé");
            PoemRandomizerHook.Load();
        }

        public override void Unload() {
            PoemRandomizerHook.Unload();
        }
    }
}