using Celeste.Mod;
using System;
using System.Linq;
using System.Reflection;

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
            DumpCollabUtils2MiniHeartTypes();
        }

        public override void Unload() {
            PoemRandomizerHook.Unload();
        }

        private static void DumpCollabUtils2MiniHeartTypes() {
            try {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                    string asmName = asm.GetName().Name ?? "";
                    if (!asmName.Contains("CollabUtils2", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Logger.Log(LogLevel.Info, "PoemRandomizer", $"Assembly trouvée: {asm.FullName}");

                    Type[] types;
                    try {
                        types = asm.GetTypes();
                    } catch (ReflectionTypeLoadException e) {
                        types = e.Types.Where(t => t != null).ToArray();
                    }

                    foreach (Type t in types.OrderBy(t => t.FullName)) {
                        string fullName = t.FullName ?? "";
                        if (!fullName.Contains("MiniHeart", StringComparison.OrdinalIgnoreCase) &&
                            !fullName.Contains("Heart", StringComparison.OrdinalIgnoreCase))
                            continue;

                        Logger.Log(LogLevel.Info, "PoemRandomizer", $"Type: {fullName}");

                        foreach (MethodInfo m in t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                            Logger.Log(LogLevel.Info, "PoemRandomizer", $"  Method: {m}");
                        }
                    }
                }
            } catch (Exception e) {
                Logger.Log(LogLevel.Warn, "PoemRandomizer", $"Erreur dump CollabUtils2: {e}");
            }
        }
    }
}