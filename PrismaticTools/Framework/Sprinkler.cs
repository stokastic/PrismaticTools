using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewModdingAPI.Events;
using System.Collections.Generic;

namespace PrismaticTools.Framework {

    // searches map for any currently placed prismatic sprinklers and:
    //   - waters adjacent tiles
    //   - enables light sources
    //   - optionally makes them act as scarecrows
    public static class SprinklerInitializer {
        
        public static void Init() {
            TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            LocationEvents.ObjectsChanged += LocationEvents_ObjectsChanged;
        }

        private static void SaveEvents_AfterLoad(object sender, System.EventArgs e) {
            if (ModEntry.Config.UseSprinklersAsScarecrows) {
                foreach (GameLocation location in Game1.locations) {
                    foreach (Object obj in location.Objects.Values) {
                        if (obj.ParentSheetIndex == PrismaticSprinklerItem.INDEX) {
                            obj.Name = "Prismatic Scarecrow Sprinkler";
                        }
                    }
                }
            }

            // set light source
            if (!ModEntry.Config.UseSprinklersAsLamps) {
                return;
            }
            Object sprinkler;
            foreach (GameLocation location in Game1.locations) {
                if (location is GameLocation) {
                    foreach (KeyValuePair<Vector2, Object> pair in location.objects.Pairs) {
                        if (location.objects[pair.Key].ParentSheetIndex == PrismaticSprinklerItem.INDEX) {
                            sprinkler = location.objects[pair.Key];
                            int id = (int)sprinkler.TileLocation.X * 4000 + (int)sprinkler.TileLocation.Y;
                            sprinkler.lightSource = new LightSource(4, new Vector2((sprinkler.boundingBox.X + 32), (sprinkler.boundingBox.Y + 32)), 2.0f, Color.Black, id);
                            location.sharedLights.Add(sprinkler.lightSource.Clone());
                        }
                    }
                }
            }
        }

        private static void LocationEvents_ObjectsChanged(object sender, EventArgsLocationObjectsChanged e) {
            // adds lightsources to newly placed sprinkler
            if (!ModEntry.Config.UseSprinklersAsLamps) {
                return;
            }
            foreach (KeyValuePair<Vector2, Object> pair in e.Added) {
                Object obj = pair.Value;
                if (obj.ParentSheetIndex == PrismaticSprinklerItem.INDEX) {
                    int id = (int)obj.TileLocation.X * 4000 + (int)obj.TileLocation.Y;
                    obj.lightSource = new LightSource(4, new Vector2((obj.boundingBox.X + 32), (obj.boundingBox.Y + 32)), 2.0f, Color.Black, id);
                    obj.Name = "Prismatic Scarecrow Sprinkler";
                    Game1.currentLocation.sharedLights.Add(obj.lightSource.Clone());
                }
            }
        }

        private static void TimeEvents_AfterDayStarted(object sender, System.EventArgs e) {
            foreach (GameLocation location in Game1.locations) {
                foreach (Object obj in location.Objects.Values) {
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

