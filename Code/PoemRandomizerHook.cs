using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste;
using Celeste.Mod;
using MonoMod.RuntimeDetour;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.PoemRandomizer {
    public static class PoemRandomizerHook {
        private static Hook _drawPoemHook;
        private static readonly Random Rng = new Random();
        private static readonly ConditionalWeakTable<Poem, Holder> ChosenPoems = new ConditionalWeakTable<Poem, Holder>();

        private static readonly string[] RandomPoems = {
            "The mountain does not care about your feelings.",
            "One step. Then another.",
            "Fear is not a wall. It is a door.",
            "You carried the weight. You can set it down.",
            "Every summit begins with a single doubt."
        };

        private sealed class Holder {
            public string Value;
        }

        public static void Load() {
            Logger.Log(LogLevel.Info, "PoemRandomizer", "Hook Load appelé");
            On.Celeste.HeartGem.Collect += OnHeartGemCollect;

            MethodInfo drawPoem = typeof(Poem).GetMethod(
                "DrawPoem",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Vector2), typeof(Color) },
                null
            );

            if (drawPoem == null) {
                Logger.Log(LogLevel.Error, "PoemRandomizer", "Poem.DrawPoem(Vector2, Color) introuvable");
                return;
            }

            _drawPoemHook = new Hook(drawPoem, OnDrawPoem);
            Logger.Log(LogLevel.Info, "PoemRandomizer", "Hook Poem.DrawPoem installé");
        }

        public static void Unload() {
            On.Celeste.HeartGem.Collect -= OnHeartGemCollect;
            _drawPoemHook?.Dispose();
            _drawPoemHook = null;
        }

        private static void OnHeartGemCollect(On.Celeste.HeartGem.orig_Collect orig, HeartGem self, Player player) {
            Logger.Log(LogLevel.Info, "PoemRandomizer", "HeartGem.Collect déclenché");
            orig(self, player);
        }

        private delegate void orig_DrawPoem(Poem self, Vector2 position, Color color);

        private static void OnDrawPoem(orig_DrawPoem orig, Poem self, Vector2 position, Color color) {
            string chosen = ChosenPoems.GetValue(self, _ => new Holder {
                Value = RandomPoems[Rng.Next(RandomPoems.Length)]
            }).Value;

            try {
                FieldInfo textField = typeof(Poem).GetField("text", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (textField != null && textField.FieldType == typeof(string)) {
                    string oldValue = textField.GetValue(self) as string;
                    if (!string.IsNullOrEmpty(oldValue) && oldValue != chosen) {
                        textField.SetValue(self, chosen);
                        Logger.Log(LogLevel.Info, "PoemRandomizer", $"Poem text fixé: {oldValue} -> {chosen}");
                    }
                }
            } catch (Exception e) {
                Logger.Log(LogLevel.Warn, "PoemRandomizer", $"Erreur remplacement texte: {e.Message}");
            }

            orig(self, position, color);
        }
    }
}