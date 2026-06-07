using Celeste.Mod;
using System;

namespace Celeste.Mod.PoemRandomizer {
    public class PoemRandomizerModule : EverestModule {
        public static PoemRandomizerModule Instance;
        public override Type SettingsType => typeof(PoemRandomizerSettings);
        public static PoemRandomizerSettings Settings => (PoemRandomizerSettings) Instance._Settings;

        public PoemRandomizerModule() {
            Instance = this;
        }

        public override void Load() {
            Instance = this;
            PoemRandomizerHook.Load();
        }

        public override void Unload() {
            PoemRandomizerHook.Unload();
        }
    }
}