using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace PrismaticTools.Framework {
    public class PrismaticAPI {
        private int SprinklerRange { get; set; } = 3;
        public int SprinklerIndex { get; set; } = PrismaticSprinklerItem.INDEX;
        public int BarIndex { get; set; } = PrismaticBarItem.INDEX;

        public IEnumerable<Vector2> GetSprinklerCoverage(Vector2 origin) {
            for (int x = -SprinklerRange; x <= SprinklerRange; x++) {
                for (int y = -SprinklerRange; y <= SprinklerRange; y++) {
                    yield return new Vector2(x, y) + origin;
                }
            }
        }
    }
}
