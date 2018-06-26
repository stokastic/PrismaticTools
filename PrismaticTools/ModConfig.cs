
namespace PrismaticTools.Framework {
    public class ModConfig {
        public bool UseSprinklersAsScarecrows { get; set; }
        public bool UseSprinklersAsLamps { get; set; }

        public ModConfig() {
            UseSprinklersAsScarecrows = true;
            UseSprinklersAsLamps = true;
        }
    }
}
