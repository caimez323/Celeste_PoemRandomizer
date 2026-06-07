using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste;
using Celeste.Mod;
using MonoMod.RuntimeDetour;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Celeste.Mod.PoemRandomizer {
    public static class PoemRandomizerHook {
        private static Hook _drawPoemHook;
        private static readonly Random Rng = new Random();
        private static readonly ConditionalWeakTable<Poem, Holder> ChosenPoems = new ConditionalWeakTable<Poem, Holder>();

        private static readonly Dictionary<PoemPool, string[]> Pools = new() {
            [PoemPool.Français] = new[] {
                "Moi ? Mais qui elle est celle là ?",
                "hahaha pff ouais c'est un peu chiant les gars en gros Luden c'est un mythique, passive mythique \nqui donne de la péné magique et donc en gros ça donne 6 de péné magique flat donc à 2 items complets.\n donc il a 10 de péné flat donc il monte à 16, il a les bottes ça fait 18.\nDonc 16+18 ça fait 34 si jdis pas de conneries donc 34 + il avait shadow flame donc il a 44\n + et après du coup le void staff faut faire 44 divisé par 0.6. en gros il fait des dégats purs\n à un mec jusqu'à 73 de RM, j'avais dit 70 dans le cast à peu près et en gros bah les mecs ils ont\n pas 70 de RM, parce que globalement y'a eu un patch, en gros y'a le patch qui fait 0.8 de RM sur les\n carrys et en gros de base sur lol y'avait pas ça. Et en gros la botlane va jamais prendre de la RM en lane.\n En tout cas pas beaucoup donc c'est pas ouf en vrai.\n Je pense que son item est nul donc en vrai j'pense soit il enlève shadowflame, soit le void staff,\n mais j'pense qu'il vaut mieux enlever shadowflame.",
                "six ou sept",
                "Plaît-il ?",
                "Moi puceau ?",
                "Ben voyons...",
                "Il m'a dit, c'est abordable, il a utilisé ces mots\nA B O R D A B L E  P U T E",
                "Mais voilà mais c'était sur enfait, c'était sur",
                "Putain de manette de merde",
                "GG !",
                "ça doit être ça ouai",
                "C'est qui le patron ? c'est moi",
                "Macron décapitation",
                "Vous pensez ? \nMoi je pense pas.\nC'est mon avis.",
                "J'adore l'eau, dans 20-30 ans y en aura plus",
                "Marge, est ce que tu m'a préparé mon donut, sucré, au sucre ?",
                "Tu préfères le chocolat ou les ",
                "Ta moustache est ratée.",
                "Sincères félicitations de la part de Sylvain-Pierre Durif.",
                "Je vais t'expliquer de suite, jsuis un nerveux.",
                "Encore ? ça fait beaucoup la non ?",
                "Est ce que c'est bon pour vous ?",
                "C'est comme si des chevals qui appellent des chevals",
                "Les coutures tiennent pas",
                "Ouai c'est pas faux",
                "Bleu chiotte comme ça mais pas ternie. Faut que ça biche quand même.",
                "Le fréro il a du se manger une clio 4 plein front",
                "Le sanglier",
                "Puff gout paf",
                "Y a les hendeks qu'arrivent",
                "Je préfère Norman",
                "Ha jsui biiiieeeeennnng",
                "Il est très salé le coeur la",
                "Ho putain Laurent !",
                "C'est ciao",
                "Elle est où Jeanne ?",
                "Cette anecdote elle est vraie ou elle est fausse ?",
                "Vous mesurez 1m40. \n Faut manger de la soupe."
            },
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
            // Si Vanilla, on ne touche rien
            if (PoemRandomizerModule.Settings.SelectedPool == PoemPool.Vanilla) {
                orig(self, position, color);
                return;
            }

            string[] pool = Pools[PoemRandomizerModule.Settings.SelectedPool];

            string chosen = ChosenPoems.GetValue(self, _ => new Holder {
                Value = pool[Rng.Next(pool.Length)]
            }).Value;

            try {
                FieldInfo textField = typeof(Poem).GetField("text",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (textField != null) {
                    string current = textField.GetValue(self) as string;
                    if (!string.IsNullOrEmpty(current) && current != chosen)
                        textField.SetValue(self, chosen);
                }
            } catch (Exception e) {
                Logger.Log(LogLevel.Warn, "PoemRandomizer", $"Erreur: {e.Message}");
            }

            orig(self, position, color);
        }
    }
}