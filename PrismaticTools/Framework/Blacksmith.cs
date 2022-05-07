using System.Collections.Generic;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace PrismaticTools.Framework {
    internal class BlacksmithInitializer {
        private static readonly int UpgradeCost = ModEntry.Config.PrismaticToolCost;

        public static void Init(IModEvents events) {
            events.Display.MenuChanged += OnMenuChanged;
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnMenuChanged(object sender, MenuChangedEventArgs e) {
            if (!(e.NewMenu is ShopMenu menu)) {
                return;
            }
            List<int> categories = ModEntry.ModHelper.Reflection.GetField<List<int>>(menu, "categoriesToSellHere").GetValue();
            if (!categories.Contains(Object.GemCategory) || !categories.Contains(Object.mineralsCategory) || !categories.Contains(Object.metalResources)) {
                return;
            }
            Farmer who = Game1.player;

            Tool toolFromName1 = who.getToolFromName("Axe");
            Tool toolFromName2 = who.getToolFromName("Watering Can");
            Tool toolFromName3 = who.getToolFromName("Pickaxe");
            Tool toolFromName4 = who.getToolFromName("Hoe");
            Tool tool;

            List<ISalable> forSale = menu.forSale;
            Dictionary<ISalable, int[]> stock = menu.itemPriceAndStock;

            if (toolFromName1 != null && toolFromName1.UpgradeLevel == 4) {
                tool = new Axe { UpgradeLevel = 5 };
                forSale.Add(tool);
                stock.Add(tool, new[] { UpgradeCost, 1, PrismaticBarItem.INDEX });
            }
            if (toolFromName2 != null && toolFromName2.UpgradeLevel == 4) {
                tool = new WateringCan { UpgradeLevel = 5 };
                forSale.Add(tool);
                stock.Add(tool, new[] { UpgradeCost, 1, PrismaticBarItem.INDEX });
            }
            if (toolFromName3 != null && toolFromName3.UpgradeLevel == 4) {
                tool = new Pickaxe { UpgradeLevel = 5 };
                forSale.Add(tool);
                stock.Add(tool, new[] { UpgradeCost, 1, PrismaticBarItem.INDEX });
            }
            if (toolFromName4 != null && toolFromName4.UpgradeLevel == 4) {
                tool = new Hoe { UpgradeLevel = 5 };
                forSale.Add(tool);
                stock.Add(tool, new[] { UpgradeCost, 1, PrismaticBarItem.INDEX });
            }
        }
    }
}
