using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Harmony;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;

using PrismaticTools.Framework;
using System.Collections.Generic;
using StardewValley.Objects;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace PrismaticTools {

    public class ModEntry : Mod {

        public static IMonitor mon;
        public static IModHelper ModHelper;
        public static ModConfig Config;

        public static Texture2D toolsTexture;
        private int colorCycleIndex = 0;
        private List<Color> colors = new List<Color>();

        public override void Entry(IModHelper helper) {
            mon = Monitor;
            ModHelper = helper;

            Config = Helper.ReadConfig<ModConfig>();

            toolsTexture = ModHelper.Content.Load<Texture2D>("Assets/tools.png", ContentSource.ModFolder);

            helper.ConsoleCommands.Add("ptools", "Upgrade all tools to prismatic", UpgradeTools);

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Player.InventoryChanged += OnInventoryChanged;

            helper.Content.AssetEditors.Add(new AssetEditor());
            SprinklerInitializer.Init(helper.Events);
            BlacksmithInitializer.Init(helper.Events);

            InitColors();

            var harmony = HarmonyInstance.Create("stokastic.PrismaticTools");
            this.ApplyPatches(harmony);
        }

        private void ApplyPatches(HarmonyInstance harmony) {
            // furnaces
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.getTallyOfObject)),
                prefix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.Farmer_GetTallyOfObject))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.performObjectDropInAction)),
                prefix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.Object_PerformObjectDropInAction))
            );

            // sprinklers
            harmony.Patch(
                original: AccessTools.Method(typeof(Farm), nameof(Farm.addCrows)),
                prefix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.Farm_AddCrows))
            );

            // tools
            harmony.Patch(
                original: AccessTools.Method(typeof(Tree), nameof(Tree.performToolAction)),
                prefix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.Tree_PerformToolAction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(FruitTree), nameof(FruitTree.performToolAction)),
                prefix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.FruitTree_PerformToolAction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Pickaxe), nameof(Pickaxe.DoFunction)),
                prefix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.Pickaxe_DoFunction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(ResourceClump), nameof(ResourceClump.performToolAction)),
                prefix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.ResourceClump_PerformToolAction))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Tool), "tilesAffected"),
                postfix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.Tool_TilesAffected_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Property(typeof(Tool), nameof(Tool.Name)).GetMethod,
                prefix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.Tool_Name))
            );
            harmony.Patch(
                original: AccessTools.Property(typeof(Tool), nameof(Tool.DisplayName)).GetMethod,
                prefix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.Tool_DisplayName))
            );
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e) {
            if (!e.IsMultipleOf(8))
                return;

            Farmer farmer = Game1.player;
            Item item;
            try {
                item = farmer.Items[farmer.CurrentToolIndex];
            }  catch (System.ArgumentOutOfRangeException) {
                return;
            }

            if (!(item is Object obj) || obj.ParentSheetIndex != PrismaticBarItem.INDEX) {
                return;
            }

            foreach (var light in farmer.currentLocation.sharedLights.Values) {
                if (light.Identifier == (int)farmer.UniqueMultiplayerID) {
                    light.color.Value = colors[colorCycleIndex];
                }
            }
            colorCycleIndex = (colorCycleIndex + 1) % colors.Count;
        }

        public override object GetApi() {
            return new PrismaticAPI();
        }

        private void UpgradeTools(string command, string[] args) {
            foreach (Item item in Game1.player.Items) {
                if (item is Axe || item is WateringCan || item is Pickaxe || item is Hoe) {
                    (item as Tool).UpgradeLevel = 5;
                }
            }
        }

        // adds lightsources to prismatic bar and sprinkler items in inventory
        private void AddLightsToInventoryItems() {
            if (!Config.UseSprinklersAsLamps) {
                return;
            }
            foreach (Item item in Game1.player.Items) {
                if (item is Object) {
                    if (item.ParentSheetIndex == PrismaticSprinklerItem.INDEX) {
                        (item as Object).lightSource = new LightSource(LightSource.cauldronLight, new Vector2(0, 0), 2.0f, new Color(0.0f, 0.0f, 0.0f));
                    } else if (item.ParentSheetIndex == PrismaticBarItem.INDEX) {
                        (item as Object).lightSource = new LightSource(LightSource.cauldronLight, new Vector2(0, 0), 1.0f, colors[colorCycleIndex]);
                    }
                }
            }
        }

        /// <summary>Raised after items are added or removed to a player's inventory.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnInventoryChanged(object sender, InventoryChangedEventArgs e) {
            if (e.IsLocalPlayer)
                AddLightsToInventoryItems();
        }

        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) {
            // force add sprinkler recipe for people who were level 10 before installing mod
            if (Game1.player.FarmingLevel >= PrismaticSprinklerItem.CRAFTING_LEVEL) {
                try {
                    Game1.player.craftingRecipes.Add("Prismatic Sprinkler", 0);
                } catch { }
            }

            IndexCompatibilityFix();
            AddLightsToInventoryItems();
        }

        // used to resolve asset conflicts with other mods
        private void IndexCompatibilityFix() {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (GameLocation location in Game1.locations) {
                // check fridge
                if (location is FarmHouse farmhouse && farmhouse.fridge.Value is Chest fridge) {
                        foreach (Item item in fridge.items) {
                            if (item?.Name != null && item.Name.Contains("Prismatic")) {
                                SwapIndex(item);
                            }
                        }
                }

                // check chests, signposts, furnaces, and placed sprinklers
                foreach (Object obj in location.Objects.Values)
                {
                    switch (obj)
                    {
                        // chest
                        case Chest chest:
                            foreach (Item item in chest.items) {
                                if (item?.Name != null && item.Name.Contains("Prismatic")) {
                                    SwapIndex(item);
                                }
                            }
                            break;

                        // sign
                        case Sign sign:
                            SwapIndex(sign.displayItem.Value);
                            break;

                        default:
                            // furnace
                            if (obj.bigCraftable.Value && obj.Name == "Furnace") {
                                SwapIndex(obj.heldObject.Value);
                            }

                            // prismatic bar/sprinkler
                            else if (obj.ParentSheetIndex == PrismaticBarItem.OLD_INDEX || obj.ParentSheetIndex == PrismaticSprinklerItem.OLD_INDEX) {
                                SwapIndex(obj);
                            }
                            break;
                    }
                }
            }

            foreach (Item item in Game1.player.Items) {
                if (item != null && item.Name.Contains("Prismatic")) {
                    SwapIndex(item);
                }
            }

            watch.Stop();
            Monitor.Log($"IndexCompatibility exec time: {watch.ElapsedMilliseconds} ms", LogLevel.Trace);
        }

        private void SwapIndex(Item item) {
            if (item == null) {
                return;
            }

            if (item.ParentSheetIndex == PrismaticBarItem.OLD_INDEX) {
                item.ParentSheetIndex = PrismaticBarItem.INDEX;
            }
            if (item.ParentSheetIndex == PrismaticSprinklerItem.OLD_INDEX) {
                item.ParentSheetIndex = PrismaticSprinklerItem.INDEX;
            }
        }

        private void InitColors() {
            int n = 24;
            for(int i=0; i<n; i++) {
                colors.Add(ColorFromHSV(360.0 * i / n, 1.0, 1.0));
            }
        }

        private Color ColorFromHSV(double hue, double saturation, double value) {
            int hi = System.Convert.ToInt32(System.Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - System.Math.Floor(hue / 60);

            value = value * 255;
            int v = System.Convert.ToInt32(value);
            int p = System.Convert.ToInt32(value * (1 - saturation));
            int q = System.Convert.ToInt32(value * (1 - f * saturation));
            int t = System.Convert.ToInt32(value * (1 - (1 - f) * saturation));

            v = 255 - v;
            p = 255 - v;
            q = 255 - q;
            t = 255 - t;

            if (hi == 0)
                return new Color(v, t, p);
            else if (hi == 1)
                return new Color(q, v, p);
            else if (hi == 2)
                return new Color(p, v, t);
            else if (hi == 3)
                return new Color(p, q, v);
            else if (hi == 4)
                return new Color(t, p, v);
            else
                return new Color(v, p, q);
        }
    }
}