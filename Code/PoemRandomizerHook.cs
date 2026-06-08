using System;
using System.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;
using Celeste;
using Celeste.Mod;
using MonoMod.RuntimeDetour;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.PoemRandomizer {
    public static class PoemRandomizerHook {
        private static Hook _drawPoemHook;
        private static Hook _miniHeartOnPlayerHook;
        private static Hook _miniHeartSmashRoutineHook;

        private static readonly Random Rng = new();
        private static readonly ConditionalWeakTable<Poem, Holder> ChosenPoems = new ConditionalWeakTable<Poem, Holder>();
        private static readonly Dictionary<string, string> RuntimePoems = new Dictionary<string, string>();

        private static readonly Dictionary<PoemPool, string[]> Pools = new() {
            [PoemPool.Français] = [
                "hahaha pff ouais c'est un peu chiant les gars en gros Luden c'est un mythique, passive mythique \nqui donne de la péné magique et donc en gros ça donne 6 de péné magique flat donc à 2 items complets.\n donc il a 10 de péné flat donc il monte à 16, il a les bottes ça fait 18.\nDonc 16+18 ça fait 34 si jdis pas de conneries donc 34 + il avait shadow flame donc il a 44\n + et après du coup le void staff faut faire 44 divisé par 0.6. en gros il fait des dégats purs\n à un mec jusqu'à 73 de RM, j'avais dit 70 dans le cast à peu près et en gros bah les mecs ils ont\n pas 70 de RM, parce que globalement y'a eu un patch, en gros y'a le patch qui fait 0.8 de RM sur les\n carrys et en gros de base sur lol y'avait pas ça. Et en gros la botlane va jamais prendre de la RM en lane.\n En tout cas pas beaucoup donc c'est pas ouf en vrai.\n Je pense que son item est nul donc en vrai j'pense soit il enlève shadowflame, soit le void staff,\n mais j'pense qu'il vaut mieux enlever shadowflame.",
                "six ou sept",
                "Plaît-il ?",
                "Moi puceau ?",
                "Ben voyons...",
                "Il m'a dit, c'est abordable,\nil a utilisé ces mots\nA B O R D A B L E  P U T E",
                "Mais voilà mais c'était sur enfait,\nc'était sur",
                "Putain de manette de merde",
                "GG !",
                "ça doit être ça ouai",
                "C'est qui le patron ? c'est moi",
                "Macron décapitation",
                "Vous pensez ? \nMoi je pense pas.\nC'est mon avis.",
                "Tu préfères le chocolat ou les ",
                "Ta moustache est ratée.",
                "Sincères félicitations de la part\nde Sylvain-Pierre Durif.",
                "Encore ? ça fait beaucoup la non ?",
                "Est ce que c'est bon pour vous ?",
                "C'est comme si des chevals\nqui appellent des chevals",
                "Les coutures tiennent pas",
                "Ouai c'est pas faux",
                "Bleu chiotte comme ça mais pas ternie.\nFaut que ca biche quand même",
                "Le fréro il a du se manger une\nclio 4 plein front",
                "Le sanglier",
                "Puff gout paf",
                "Y a les hendeks qu'arrivent",
                "Je préfère Norman",
                "Ha jsui biiiieeeeennnng",
                "Il est très salé le coeur la",
                "Ho putain Laurent !",
                "C'est ciao",
                "Elle est où Jeanne ?",
                "Touche de l'herbe",
                "Clavier ou manette ?"
            ],
            [PoemPool.English] =
            [
                "Go touch some grass",
                "Keyboard or controller ?"
            ]
        };

        private sealed class Holder {
            public string Value;
        }

        private delegate void orig_DrawPoem(Poem self, Vector2 position, Color color);
        private delegate void orig_AbstractMiniHeart_onPlayer(object self, Player player);
        private delegate IEnumerator orig_MiniHeart_SmashRoutine(object self, Player player, Level level);

        public static void Load() {
            On.Celeste.HeartGem.Collect += OnHeartGemCollect;

            MethodInfo drawPoem = typeof(Poem).GetMethod(
                "DrawPoem",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                [typeof(Vector2), typeof(Color)],
                null
            );

            if (drawPoem != null) {
                _drawPoemHook = new Hook(drawPoem, OnDrawPoem);
            } else {
            }

            TryHookMiniHeartOnPlayer();
            TryHookMiniHeartSmashRoutine();
        }

        public static void Unload() {
            On.Celeste.HeartGem.Collect -= OnHeartGemCollect;

            _drawPoemHook?.Dispose();
            _drawPoemHook = null;

            _miniHeartOnPlayerHook?.Dispose();
            _miniHeartOnPlayerHook = null;

            _miniHeartSmashRoutineHook?.Dispose();
            _miniHeartSmashRoutineHook = null;

            RuntimePoems.Clear();
        }

        private static void TryHookMiniHeartOnPlayer() {
            try {
                Type abstractMiniHeartType = Type.GetType(
                    "Celeste.Mod.CollabUtils2.Entities.AbstractMiniHeart, CollabUtils2",
                    false
                );
                if (abstractMiniHeartType == null) {
                    return;
                }

                MethodInfo onPlayer = abstractMiniHeartType.GetMethod(
                    "onPlayer",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    [typeof(Player)],
                    null
                );
                if (onPlayer == null) {
                    return;
                }

                _miniHeartOnPlayerHook = new Hook(onPlayer, OnMiniHeartOnPlayer);
            } catch (Exception e) {
                Logger.Log(LogLevel.Warn, "PoemRandomizer", $"Erreur hook onPlayer: {e}");
            }
        }

        private static void TryHookMiniHeartSmashRoutine() {
            try {
                Type miniHeartType = Type.GetType(
                    "Celeste.Mod.CollabUtils2.Entities.MiniHeart, CollabUtils2",
                    false
                );
                if (miniHeartType == null) {
                    return;
                }

                MethodInfo smashRoutine = miniHeartType.GetMethod(
                    "SmashRoutine",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Player), typeof(Level) },
                    null
                );
                if (smashRoutine == null) {
                    return;
                }

                _miniHeartSmashRoutineHook = new Hook(smashRoutine, OnSmashRoutine);
            } catch (Exception e) {
                Logger.Log(LogLevel.Warn, "PoemRandomizer", $"Erreur hook SmashRoutine: {e}");
            }
        }

        private static void OnHeartGemCollect(On.Celeste.HeartGem.orig_Collect orig, HeartGem self, Player player) {
            orig(self, player);
        }

        private static void OnMiniHeartOnPlayer(orig_AbstractMiniHeart_onPlayer orig, object self, Player player) {
            orig(self, player);
        }

private static IEnumerator OnSmashRoutine(orig_MiniHeart_SmashRoutine orig, object self, Player player, Level level) {
    IEnumerator routine = orig(self, player, level);

    while (routine.MoveNext()) {
        object current = routine.Current;
        if (Engine.TimeRate < 0.5f) {
            yield return current;
            break;
        }
        yield return current;
    }

    float freeSlowTime = 0.20f;
    float elapsed = 0f;

    while (elapsed < freeSlowTime) {
        if (routine.MoveNext()) {
            yield return routine.Current;
        } else {
            yield break;
        }
        elapsed += Engine.DeltaTime;
    }

    int oldState = player != null ? player.StateMachine.State : 0;
    Vector2 lockedPosition = player != null ? player.Position : Vector2.Zero;

    if (player != null) {
        player.StateMachine.State = Player.StDummy;
        player.Speed = Vector2.Zero;
        player.Position = lockedPosition;
    }

    if (PoemRandomizerModule.Settings.SelectedPool != PoemPool.Vanilla) {
        string text = GetRandomText();
        if (!string.IsNullOrWhiteSpace(text)) {
            Poem poem = null;
            string key = "POEMRANDO_" + Guid.NewGuid().ToString("N").ToUpperInvariant();
            RuntimePoems[key] = text;
            //Couleur du mini heart ici
            int colorId = GetPoemColorId();
            try {
                poem = new Poem(key, colorId, 1f);
                level?.Add(poem);
            } catch (Exception e) {
                Logger.Log(LogLevel.Warn, "PoemRandomizer", $"Erreur création Poem: {e}");
            }

            yield return null;

            while (!Input.MenuConfirm.Pressed && !Input.Jump.Pressed && !Input.Dash.Pressed) {
                if (player != null) {
                    player.StateMachine.State = Player.StDummy;
                    player.Speed = Vector2.Zero;
                    player.Position = lockedPosition;
                }
                yield return null;
            }

            if (poem != null)
                poem.RemoveSelf();

            RuntimePoems.Remove(key);
        }
    }

    if (player != null) {
        player.StateMachine.State = oldState;
        player.Speed = Vector2.Zero;
    }

    while (routine.MoveNext()) {
        yield return routine.Current;
    }
}

        private static int GetPoemColorId() {
            return PoemRandomizerModule.Settings.SelectedColor switch {
                PoemColor.Red => 1,
                PoemColor.Gold => 2,
                PoemColor.Random => Rng.Next(0, 3),
                _ => 0
            };
        }
        private static void OnDrawPoem(orig_DrawPoem orig, Poem self, Vector2 position, Color color) {
            try {
                FieldInfo textField = typeof(Poem).GetField(
                    "text",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                if (textField != null) {
                    string current = textField.GetValue(self) as string;

                    if (!string.IsNullOrEmpty(current) && RuntimePoems.TryGetValue(current, out string runtimeText)) {
                        textField.SetValue(self, runtimeText);
                    } else if (PoemRandomizerModule.Settings.SelectedPool != PoemPool.Vanilla &&
                               Pools.TryGetValue(PoemRandomizerModule.Settings.SelectedPool, out string[] pool) &&
                               pool.Length > 0) {
                        string chosen = ChosenPoems.GetValue(self, _ => new Holder {
                            Value = pool[Rng.Next(pool.Length)]
                        }).Value;

                        if (!string.IsNullOrEmpty(current) && current != chosen) {
                            textField.SetValue(self, chosen);
                        }
                    }
                }
            } catch (Exception e) {
                Logger.Log(LogLevel.Warn, "PoemRandomizer", $"Erreur DrawPoem: {e.Message}");
            }

            orig(self, position, color);
        }

        private static string GetRandomText() {
            if (!Pools.TryGetValue(PoemRandomizerModule.Settings.SelectedPool, out string[] pool) || pool.Length == 0)
                return null;

            return pool[Rng.Next(pool.Length)];
        }
    }
}