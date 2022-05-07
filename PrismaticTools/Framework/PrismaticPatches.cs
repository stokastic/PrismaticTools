using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace PrismaticTools.Framework {
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class PrismaticPatches {
        /*********
        ** Public methods
        *********/
        /****
        ** Furnace patches
        ****/
        public static void Farmer_GetTallyOfObject(ref int __result, int index, bool bigCraftable) {
            if (index == 382 && !bigCraftable && __result <= 0)
                __result = 666666;
        }

        public static bool Object_PerformObjectDropInAction(ref SObject __instance, ref bool __result, ref Item dropInItem, bool probe, Farmer who) {
            if (!(dropInItem is SObject object1))
                return false;

            if (object1.ParentSheetIndex != 74)
                return true;

            if (__instance.name.Equals("Furnace")) {
                if (who.IsLocalPlayer && who.getTallyOfObject(382, false) == 666666) {
                    if (!probe && who.IsLocalPlayer)
                        Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.12772"));
                    return false;
                }
                if (__instance.heldObject.Value == null && !probe) {
                    __instance.heldObject.Value = new SObject(PrismaticBarItem.INDEX, 5);
                    __instance.MinutesUntilReady = 2400;
                    who.currentLocation.playSound("furnace");
                    __instance.initializeLightSource(__instance.TileLocation);
                    __instance.showNextIndex.Value = true;

                    Multiplayer multiplayer = ModEntry.ModHelper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
                    multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite(30, __instance.TileLocation * 64f + new Vector2(0.0f, -16f), Color.White, 4, false, 50f, 10, 64, (float)((__instance.TileLocation.Y + 1.0) * 64.0 / 10000.0 + 9.99999974737875E-05)) {
                        alphaFade = 0.005f
                    });
                    for (int index = who.Items.Count - 1; index >= 0; --index) {
                        if (who.Items[index] is SObject obj && obj.ParentSheetIndex == 382) {
                            --who.Items[index].Stack;
                            if (who.Items[index].Stack <= 0) {
                                who.Items[index] = null;
                                break;
                            }
                            break;
                        }
                    }
                    object1.Stack -= 1;
                    __result = object1.Stack <= 0;
                    return false;
                }
                if (__instance.heldObject.Value == null & probe) {
                    if (object1.ParentSheetIndex == 74) {
                        __instance.heldObject.Value = new SObject();
                        __result = true;
                        return false;
                    }
                }
            }
            __result = false;
            return false;
        }

        /****
        ** Sprinkler patches
        ****/
        public static bool Farm_AddCrows(ref Farm __instance) {
            int num1 = 0;
            foreach (KeyValuePair<Vector2, TerrainFeature> pair in __instance.terrainFeatures.Pairs) {
                if (pair.Value is HoeDirt dirt && dirt.crop != null)
                    ++num1;
            }
            List<Vector2> vector2List = new List<Vector2>();
            foreach (KeyValuePair<Vector2, SObject> pair in __instance.objects.Pairs) {
                if (pair.Value.Name.Contains("arecrow")) {
                    vector2List.Add(pair.Key);
                }
            }
            int num2 = System.Math.Min(4, num1 / 16);
            for (int index1 = 0; index1 < num2; ++index1) {
                if (Game1.random.NextDouble() < 1.0) {
                    for (int index2 = 0; index2 < 10; ++index2) {
                        Vector2 key = __instance.terrainFeatures.Pairs.ElementAt(Game1.random.Next(__instance.terrainFeatures.Count())).Key;
                        if (__instance.terrainFeatures[key] is HoeDirt dirt && dirt.crop?.currentPhase.Value > 1) {
                            bool flag = false;
                            foreach (Vector2 index3 in vector2List) {
                                if (Vector2.Distance(index3, key) < 9.0) {
                                    flag = true;
                                    ++__instance.objects[index3].SpecialVariable;
                                    break;
                                }
                            }
                            if (!flag)
                                dirt.crop = null;
                            break;
                        }
                    }
                }
            }
            return false;
        }

        public static void After_Object_IsSprinkler(ref SObject __instance, ref bool __result) {
            if (__instance.ParentSheetIndex == PrismaticSprinklerItem.INDEX)
                __result = true;
        }

        public static void After_Object_GetBaseRadiusForSprinkler(ref SObject __instance, ref int __result) {
            if (__instance.ParentSheetIndex == PrismaticSprinklerItem.INDEX)
                __result = ModEntry.Config.SprinklerRange;
        }

        public static bool Object_UpdatingWhenCurrentLocation(ref SObject __instance, GameTime time, GameLocation environment) {
            var obj = __instance;

            // enable sprinkler scarecrow/light
            if (obj.ParentSheetIndex == PrismaticSprinklerItem.INDEX)
                TryEnablePrismaticSprinkler(environment, obj.TileLocation, obj);

            return true;
        }

        public static bool Object_OnPlacing(ref SObject __instance, GameLocation location, int x, int y) {
            var obj = __instance;

            // enable sprinkler scarecrow/light
            if (obj.ParentSheetIndex == PrismaticSprinklerItem.INDEX)
                TryEnablePrismaticSprinkler(location, new Vector2(x, y), obj);

            return true;
        }

        /****
        ** Tool patches
        ****/
        public static void Tree_PerformToolAction(ref Tree __instance, Tool t, int explosion) {
            if (t is Axe axe && axe.UpgradeLevel == 5 && explosion <= 0 && ModEntry.ModHelper.Reflection.GetField<NetFloat>(__instance, "health").GetValue() > -99f) {
                __instance.health.Value = 0.0f;
            }
        }

        public static void FruitTree_PerformToolAction(ref FruitTree __instance, Tool t, int explosion) {
            if (t is Axe axe && axe.UpgradeLevel == 5 && explosion <= 0 && ModEntry.ModHelper.Reflection.GetField<NetFloat>(__instance, "health").GetValue() > -99f) {
                __instance.health.Value = 0.0f;
            }
        }

        public static void Pickaxe_DoFunction(ref Pickaxe __instance, GameLocation location, int x, int y, int power, Farmer who) {
            if (__instance.UpgradeLevel == 5) {
                if (location.Objects.TryGetValue(new Vector2(x / 64, y / 64), out SObject obj)) {
                    if (obj.Name == "Stone") {
                        obj.MinutesUntilReady = 0;
                    }
                }
            }
        }

        public static void ResourceClump_PerformToolAction(ref ResourceClump __instance, Tool t, int damage, Vector2 tileLocation, GameLocation location) {
            if (t is Axe && t.UpgradeLevel == 5 && (__instance.parentSheetIndex.Value == 600 || __instance.parentSheetIndex.Value == 602)) {
                __instance.health.Value = 0;
            }
        }

        public static void Tool_TilesAffected_Postfix(ref List<Vector2> __result, Vector2 tileLocation, int power, Farmer who) {
            if (power >= 6) {
                __result.Clear();
                Vector2 direction;
                Vector2 orth;
                int radius = ModEntry.Config.PrismaticToolWidth;
                int length = ModEntry.Config.PrismaticToolLength;
                switch (who.FacingDirection) {
                    case 0: direction = new Vector2(0, -1); orth = new Vector2(1, 0); break;
                    case 1: direction = new Vector2(1, 0); orth = new Vector2(0, 1); break;
                    case 2: direction = new Vector2(0, 1); orth = new Vector2(-1, 0); break;
                    case 3: direction = new Vector2(-1, 0); orth = new Vector2(0, -1); break;
                    default: direction = Vector2.Zero; orth = Vector2.Zero; break;
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

        public static bool Tool_Name(Tool __instance, ref string __result) {
            if (__instance.UpgradeLevel == 5) {

                switch (__instance.BaseName) {
                    case "Axe": __result = ModEntry.ModHelper.Translation.Get("prismaticAxe"); break;
                    case "Pickaxe": __result = ModEntry.ModHelper.Translation.Get("prismaticPickaxe"); break;
                    case "Watering Can": __result = ModEntry.ModHelper.Translation.Get("prismaticWatercan"); break;
                    case "Hoe": __result = ModEntry.ModHelper.Translation.Get("prismaticHoe"); break;
                }
                //__result = "Prismatic " + __instance.BaseName;
                //__result = ModEntry.ModHelper.Translation.Get("prismatic.prefix") + " " + Game1.content.LoadString("Strings\\StringsFromCSFiles:Axe.cs.1");
                return false;
            }
            return true;
        }

        public static bool Tool_DisplayName(Tool __instance, ref string __result) {
            if (__instance.UpgradeLevel == 5) {
                __result = __instance.Name;
                return false;
            }
            return true;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Try to add the light source for a prismatic sprinkler, if applicable.</summary>
        /// <param name="location">The location containing the sprinkler.</param>
        /// <param name="tile">The sprinkler's tile coordinate within the location.</param>
        /// <param name="obj">The object to check.</param>
        private static void TryEnablePrismaticSprinkler(GameLocation location, Vector2 tile, SObject obj) {
            if (obj.ParentSheetIndex != PrismaticSprinklerItem.INDEX)
                return;

            // set name
            obj.Name = ModEntry.Config.UseSprinklersAsScarecrows
                ? "Prismatic Scarecrow Sprinkler"
                : "Prismatic Sprinkler";

            // add light source
            if (ModEntry.Config.UseSprinklersAsLamps) {
                int id = (int)tile.X * 4000 + (int)tile.Y;
                if (!location.sharedLights.ContainsKey(id)) {
                    obj.lightSource = new LightSource(4, tile * Game1.tileSize, 2.0f, Color.Black, id);
                    location.sharedLights.Add(id, obj.lightSource);
                }
            }
        }
    }
}
