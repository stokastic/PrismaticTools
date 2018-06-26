using Harmony;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Tools;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.TerrainFeatures;
using Netcode;

namespace PrismaticTools.Framework {

    class ToolInitializer {
        public static void Init() {
            InputEvents.ButtonPressed += InputEvents_ButtonPressed;
        }

        private static void InputEvents_ButtonPressed(object sender, EventArgsInput e) {
            if (!Context.IsWorldReady || e.Button != SButton.MouseLeft) {
                return;
            }
            Vector2 tileLocation = e.Cursor.GrabTile;
            Tool tool = Game1.player.CurrentTool;

            if (tool is Pickaxe && tool.UpgradeLevel == 5) {
                if (Game1.currentLocation.Objects.TryGetValue(tileLocation, out Object obj)) {
                    if (obj.Name == "Stone") {
                        obj.MinutesUntilReady = 0;
                    }
                }
            }
            if (tool is Axe && tool.UpgradeLevel == 5) {
                if (Game1.currentLocation.terrainFeatures.TryGetValue(tileLocation, out TerrainFeature terrainFeature)) {
                    if (terrainFeature is Tree) {
                        ModEntry.ModHelper.Reflection.GetField<NetFloat>((Tree)terrainFeature, "health").SetValue(new NetFloat(0.0f));
                    } else if (terrainFeature is FruitTree) {
                        ModEntry.ModHelper.Reflection.GetField<NetFloat>((FruitTree)terrainFeature, "health").SetValue(new NetFloat(0.0f));
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Tool), "tilesAffected")]
    internal class PrismaticTilesAffected {
        static void Postfix(ref List<Vector2> __result, Vector2 tileLocation, int power, Farmer who) {
            if (power >= 6) {
                __result.Clear();
                Vector2 direction;
                Vector2 orth;
                int radius = 2;
                int length = 7;
                switch (who.FacingDirection) {
                    case 0: direction = new Vector2(0, -1); orth = new Vector2(1, 0); break;
                    case 1: direction = new Vector2(1, 0);  orth = new Vector2(0, 1); break;
                    case 2: direction = new Vector2(0, 1);  orth = new Vector2(-1, 0); break;
                    case 3: direction = new Vector2(-1, 0); orth = new Vector2(0, -1); break;
                    default: direction = new Vector2(0, 0); orth = new Vector2(0, 0); break;
                }
                for (int i = 0; i < length; i++) {
                    __result.Add(direction * i + tileLocation);
                    for (int j = 1; j <= radius; j++) {
                        __result.Add(direction * i + orth * j + tileLocation);
                        __result.Add(direction * i + orth * -j + tileLocation);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Tool), "get_Name")]
    internal class PrismaticGetName {
        public static bool Prefix(Tool __instance, ref string __result) {
            if (__instance.UpgradeLevel == 5) {
                __result = "Prismatic " + __instance.BaseName;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Tool), "get_DisplayName")]
    internal static class CobaltDisplayNameHook {
        public static bool Prefix(Tool __instance, ref string __result) {
            if (__instance.UpgradeLevel == 5) {
                __result = __instance.Name;
                return false;
            }
            return true;
        }
    }
}
