using System;
using Celeste.Mod.LollydesSJContributions.Entities;
using Monocle;

namespace Celeste.Mod.LollydesSJContributions {
    public class MainPlogon : EverestModule {
        public static MainPlogon Instance { get; private set; }

        public static SpriteBank SpriteBank => Instance._CustomEntitySpriteBank;
        public static bool requireSjInterop { get => Everest.Loader.DependencyLoaded(strawberryJam); }


        private static EverestModuleMetadata strawberryJam = new EverestModuleMetadata()
        {
            Name = "StrawberryJam2021",
            Version = new Version(1, 0, 9)
        };
        private SpriteBank _CustomEntitySpriteBank;

        public MainPlogon() {
            Instance = this;
        }

        public override void Load() {
            Entities.BoostRose.Load();
            Entities.PocketUmbrellaController.Load();
        }

        public override void Unload() {
            Entities.BoostRose.Unload();
            Entities.PocketUmbrellaController.Unload();
        }

        public override void LoadContent(bool firstLoad)
        {
            base.LoadContent(firstLoad);

            _CustomEntitySpriteBank = new SpriteBank(GFX.Game, "Graphics/lollyde_sj/CustomEntitySprites.xml");
            PocketUmbrella.LoadParticles();
            MaskedOutline.LoadTexture();
        }
    }

    static public class CustomSFX
    {
        public static readonly string BoostRoseMovement = "event:/lollyde-triple-boost-flower/glider_movement";
    }
}