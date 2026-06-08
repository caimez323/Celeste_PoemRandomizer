using Celeste.Mod;

namespace Celeste.Mod.PoemRandomizer {
    public class PoemRandomizerSettings : EverestModuleSettings {
        [SettingName("POEMRANDOMIZER_POOL")]
        public PoemPool SelectedPool { get; set; } = PoemPool.Français;

        [SettingName("POEMRANDOMIZER_MINI_HEART_COLOR")]
        public PoemColor SelectedColor { get; set; } = PoemColor.Blue;
    }

    public enum PoemPool {
        Français,
        English,
        Vanilla
    }

    public enum PoemColor {
        Blue,
        Red,
        Gold,
        Random
    }
}