using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PrismaticTools.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace PrismaticTools {
    public class ModEntry : Mod {
        public static IModHelper ModHelper;
        public static ModConfig Config;
        public static Texture2D ToolsTexture;

        private int colorCycleIndex;
        private readonly List<Color> colors = new List<Color>();
        private AssetEditor AssetEditor;

        public override void Entry(IModHelper helper) {
            ModHelper = helper;
            this.AssetEditor = new AssetEditor();

            Config = this.Helper.ReadConfig<ModConfig>();

            ToolsTexture = ModHelper.ModContent.Load<Texture2D>("assets/tools.png");

            helper.ConsoleCommands.Add("ptools", "Upgrade all tools to prismatic", this.UpgradeTools);

            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;

            BlacksmithInitializer.Init(helper.Events);

            this.InitColors();

            var harmony = new Harmony("stokastic.PrismaticTools");
            this.ApplyPatches(harmony);
        }

        private void ApplyPatches(Harmony harmony) {
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
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.IsSprinkler)),
                postfix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.After_Object_IsSprinkler))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.GetBaseRadiusForSprinkler)),
                postfix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.After_Object_GetBaseRadiusForSprinkler))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.updateWhenCurrentLocation)),
                prefix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.Object_UpdatingWhenCurrentLocation))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.placementAction)),
                prefix: new HarmonyMethod(typeof(PrismaticPatches), nameof(PrismaticPatches.Object_OnPlacing))
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
            } catch (System.ArgumentOutOfRangeException) {
                return;
            }

            if (!(item is Object obj) || obj.ParentSheetIndex != PrismaticBarItem.INDEX) {
                return;
            }

            foreach (var light in farmer.currentLocation.sharedLights.Values) {
                if (light.Identifier == (int)farmer.UniqueMultiplayerID) {
                    light.color.Value = this.colors[this.colorCycleIndex];
                }
            }
            this.colorCycleIndex = (this.colorCycleIndex + 1) % this.colors.Count;
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

        /// <summary>Adds light sources to prismatic bar and sprinkler items in inventory.</summary>
        private void AddLightsToInventoryItems() {
            if (!Config.UseSprinklersAsLamps) {
                return;
            }
            foreach (Item item in Game1.player.Items) {
                if (item is Object obj) {
                    if (obj.ParentSheetIndex == PrismaticSprinklerItem.INDEX) {
                        obj.lightSource = new LightSource(LightSource.cauldronLight, Vector2.Zero, 2.0f, new Color(0.0f, 0.0f, 0.0f));
                    } else if (obj.ParentSheetIndex == PrismaticBarItem.INDEX) {
                        obj.lightSource = new LightSource(LightSource.cauldronLight, Vector2.Zero, 1.0f, this.colors[this.colorCycleIndex]);
                    }
                }
            }
        }

        /// <summary>Set scarecrow mode for sprinkler items.</summary>
        private void SetScarecrowModeForAllSprinklers() {
            foreach (GameLocation location in Game1.locations) {
                foreach (Object obj in location.Objects.Values) {
                    if (obj.ParentSheetIndex == PrismaticSprinklerItem.INDEX) {
                        obj.Name = Config.UseSprinklersAsScarecrows
                            ? "Prismatic Scarecrow Sprinkler"
                            : "Prismatic Sprinkler";
                    }
                }
            }
        }

        /// <summary>Raised after items are added or removed to a player's inventory.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnInventoryChanged(object sender, InventoryChangedEventArgs e) {
            if (e.IsLocalPlayer)
                this.AddLightsToInventoryItems();
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

            this.AddLightsToInventoryItems();
            this.SetScarecrowModeForAllSprinklers();
        }

        /// <summary>Raised when an asset is being requested from the content pipeline.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnAssetRequested(object sender, AssetRequestedEventArgs e) {
            this.AssetEditor.OnAssetRequested(e);
        }

        private void InitColors() {
            int n = 24;
            for (int i = 0; i < n; i++) {
                this.colors.Add(this.ColorFromHSV(360.0 * i / n, 1.0, 1.0));
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

            switch (hi) {
                case 0:
                    return new Color(v, t, p);
                case 1:
                    return new Color(q, v, p);
                case 2:
                    return new Color(p, v, t);
                case 3:
                    return new Color(p, q, v);
                case 4:
                    return new Color(t, p, v);
                default:
                    return new Color(v, p, q);
            }
        }
    }
}
