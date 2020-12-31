using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PrismaticTools.Framework {
    public class PrismaticAPI {
        public int SprinklerRange { get; } = ModEntry.Config.SprinklerRange;
        public int SprinklerIndex { get; } = PrismaticSprinklerItem.INDEX;
        public int BarIndex { get; } = PrismaticBarItem.INDEX;
        public bool ArePrismaticSprinklersScarecrows { get; } = ModEntry.Config.UseSprinklersAsScarecrows;

        public IEnumerable<Vector2> GetSprinklerCoverage(Vector2 origin) {
            for (int x = -this.SprinklerRange; x <= this.SprinklerRange; x++) {
                for (int y = -this.SprinklerRange; y <= this.SprinklerRange; y++)
                    yield return new Vector2(x, y) + origin;
            }
        }
    }
}
