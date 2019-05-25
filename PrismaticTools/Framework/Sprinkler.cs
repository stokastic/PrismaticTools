using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace PrismaticTools.Framework {
    // searches map for any currently placed prismatic sprinklers and:
    //   - waters adjacent tiles
    //   - enables light sources
    //   - optionally makes them act as scarecrows
    public static class SprinklerInitializer {
        public static void Init(IModEvents events) {
            events.GameLoop.DayStarted += OnDayStarted;
            events.GameLoop.SaveLoaded += OnSaveLoaded;
            events.World.ObjectListChanged += OnObjectListChanged;
        }

        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnSaveLoaded(object sender, SaveLoadedEventArgs e) {
            if (ModEntry.Config.UseSprinklersAsScarecrows) {
                foreach (GameLocation location in Game1.locations) {
                    foreach (SObject obj in location.Objects.Values) {
                        if (obj.ParentSheetIndex == PrismaticSprinklerItem.INDEX) {
                            obj.Name = "Prismatic Scarecrow Sprinkler";
                        }
                    }
                }
            }

            // set light sources
            if (ModEntry.Config.UseSprinklersAsLamps) {
                foreach (GameLocation location in Game1.locations) {
                    foreach (KeyValuePair<Vector2, SObject> pair in location.objects.Pairs)
                        TryEnablePrismaticSprinkler(location, pair.Key, pair.Value);
                }
            }
        }

        /// <summary>Raised after objects are added or removed in a location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnObjectListChanged(object sender, ObjectListChangedEventArgs e) {
            // adds light sources to newly placed sprinklers
            if (ModEntry.Config.UseSprinklersAsLamps) {
                foreach (KeyValuePair<Vector2, SObject> pair in e.Added)
                    TryEnablePrismaticSprinkler(e.Location, pair.Key, pair.Value);
            }
        }

        /// <summary>Try to add the light source for a prismatic sprinkler, if applicable.</summary>
        /// <param name="location">The location containing the sprinkler.</param>
        /// <param name="tile">The sprinkler's tile coordinate within the location.</param>
        /// <param name="obj">The object to check.</param>
        private static void TryEnablePrismaticSprinkler(GameLocation location, Vector2 tile, SObject obj) {
            if (obj.ParentSheetIndex != PrismaticSprinklerItem.INDEX || !ModEntry.Config.UseSprinklersAsLamps)
                return;

            // set name
            obj.Name = "Prismatic Scarecrow Sprinkler";

            // add light source
            int id = (int)tile.X * 4000 + (int)tile.Y;
            if (!location.sharedLights.ContainsKey(id)) {
                obj.lightSource = new LightSource(4, tile * Game1.tileSize, 2.0f, Color.Black, id);
                location.sharedLights.Add(id, obj.lightSource);
            }
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnDayStarted(object sender, DayStartedEventArgs e) {
            foreach (GameLocation location in Game1.locations) {
                foreach (SObject obj in location.Objects.Values) {
                    if (obj.ParentSheetIndex == PrismaticSprinklerItem.INDEX) {

                        // add water spray animation
                        location.TemporarySprites.Add(new TemporaryAnimatedSprite("TileSheets\\animations", new Rectangle(0, 2176, 320, 320), 60f, 4, 100, obj.TileLocation * 64 + new Vector2(-192, -208), false, false) {
                            color = Color.White * 0.4f,
                            scale = 7f / 5f,
                            delayBeforeAnimationStart = 0,
                            id = obj.TileLocation.X * 4000f + obj.TileLocation.Y
                        });

                        if (location is Farm || location.IsGreenhouse) {
                            for (int index1 = (int)obj.TileLocation.X - ModEntry.Config.SprinklerRange; index1 <= obj.TileLocation.X + ModEntry.Config.SprinklerRange; ++index1) {
                                for (int index2 = (int)obj.TileLocation.Y - ModEntry.Config.SprinklerRange; index2 <= obj.TileLocation.Y + ModEntry.Config.SprinklerRange; ++index2) {
                                    Vector2 key = new Vector2(index1, index2);

                                    // water dirt
                                    if (location.terrainFeatures.ContainsKey(key) && location.terrainFeatures[key] is HoeDirt) {
                                        (location.terrainFeatures[key] as HoeDirt).state.Value = 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
