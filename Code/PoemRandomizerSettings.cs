using Celeste.Mod;

namespace Celeste.Mod.PoemRandomizer {
    public class PoemRandomizerSettings : EverestModuleSettings {
        [SettingName("POEMRANDOMIZER_POOL")]
        public PoemPool SelectedPool { get; set; } = PoemPool.Français;
    }

    public enum PoemPool {
        Français,
        Vanilla // garde les originaux
    }
}