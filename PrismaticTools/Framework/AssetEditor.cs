using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace PrismaticTools.Framework {
    public class AssetEditor : IAssetEditor {
        private readonly string barName = ModEntry.ModHelper.Translation.Get("prismaticBar.name");
        private readonly string barDesc = ModEntry.ModHelper.Translation.Get("prismaticBar.description");
        private readonly string sprinklerName = ModEntry.ModHelper.Translation.Get("prismaticSprinkler.name");
        private readonly string sprinklerDesc = ModEntry.ModHelper.Translation.Get("prismaticSprinkler.description");

        public bool CanEdit<T>(IAssetInfo asset) {
            bool canEdit =
                   asset.AssetNameEquals("Maps/springobjects")
                || asset.AssetNameEquals("Data/ObjectInformation")
                || asset.AssetNameEquals("Data/CraftingRecipes")
                || asset.AssetNameEquals("TileSheets/tools");
            return canEdit;
        }

        public void Edit<T>(IAssetData asset) {

            if (asset.AssetNameEquals("Maps/springobjects")) {
                Texture2D bar = ModEntry.ModHelper.Content.Load<Texture2D>("Assets/prismaticBar.png");
                Texture2D sprinkler = ModEntry.ModHelper.Content.Load<Texture2D>("Assets/prismaticSprinkler.png");
                Texture2D old = asset.AsImage().Data;
                asset.ReplaceWith(new Texture2D(Game1.graphics.GraphicsDevice, old.Width, System.Math.Max(old.Height, 1200 / 24 * 16)));
                asset.AsImage().PatchImage(old);
                asset.AsImage().PatchImage(bar, targetArea: this.GetRectangle(PrismaticBarItem.INDEX));
                asset.AsImage().PatchImage(sprinkler, targetArea: this.GetRectangle(PrismaticSprinklerItem.INDEX));
            } else if (asset.AssetNameEquals("Data/ObjectInformation")) {
                asset.AsDictionary<int, string>().Data.Add(PrismaticBarItem.INDEX, $"{this.barName}/{PrismaticBarItem.PRICE}/{PrismaticBarItem.EDIBILITY}/{PrismaticBarItem.TYPE} {PrismaticBarItem.CATEGORY}/{this.barName}/{this.barDesc}");
                asset.AsDictionary<int, string>().Data.Add(PrismaticSprinklerItem.INDEX, $"{this.sprinklerName}/{PrismaticSprinklerItem.PRICE}/{PrismaticSprinklerItem.EDIBILITY}/{PrismaticSprinklerItem.TYPE} {PrismaticSprinklerItem.CATEGORY}/{this.sprinklerName}/{this.sprinklerDesc}");
            } else if (asset.AssetNameEquals("Data/CraftingRecipes")) {
                IAssetDataForDictionary<string, string> oldDict = asset.AsDictionary<string, string>();
                Dictionary<string, string> newDict = new Dictionary<string, string>();
                // somehow the Dictionary maintains ordering, so reconstruct it with new sprinkler recipe immediately after prismatic
                foreach (string key in oldDict.Data.Keys) {
                    newDict.Add(key, oldDict.Data[key]);
                    if (key.Equals("Iridium Sprinkler")) {
                        if (asset.Locale != "en")
                            newDict.Add("Prismatic Sprinkler", $"{PrismaticBarItem.INDEX} 2 787 2/Home/{PrismaticSprinklerItem.INDEX}/false/Farming {PrismaticSprinklerItem.CRAFTING_LEVEL}/{this.sprinklerName}");
                        else
                            newDict.Add("Prismatic Sprinkler", $"{PrismaticBarItem.INDEX} 2 787 2/Home/{PrismaticSprinklerItem.INDEX}/false/Farming {PrismaticSprinklerItem.CRAFTING_LEVEL}");
                    }
                }
                asset.AsDictionary<string, string>().Data.Clear();
                foreach (string key in newDict.Keys) {
                    asset.AsDictionary<string, string>().Data.Add(key, newDict[key]);
                }
            } else if (asset.AssetNameEquals("TileSheets\\tools")) {
                asset.AsImage().PatchImage(ModEntry.ToolsTexture, null, null, PatchMode.Overlay);

            }
        }

        public Rectangle GetRectangle(int id) {
            int x = (id % 24) * 16;
            int y = (id / 24) * 16;
            return new Rectangle(x, y, 16, 16);
        }
    }
}
