using Celeste.Mod.Entities;
using Celeste.Mod.LollydesSJContributions.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.LollydesSJContributions.Triggers
{
    [CustomEntity("lollyde_sj/PocketUmbrellaTrigger")]
    class PocketUmbrellaTrigger : Trigger
    {
        private bool Enable = true, revertOnLeave = false, prevVal;
        private float prevCost, prevCooldown;
        private float cooldown = 0.2f;
        private float staminaCost = 36.363636f;
        private string musicParam;

        public PocketUmbrellaTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            Enable = data.Bool("enabled", true);
            revertOnLeave = data.Bool("revertOnLeave", false);
            musicParam = data.Attr("musicParam", "none");
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            PocketUmbrellaController controller = Engine.Scene.Tracker.GetEntity<PocketUmbrellaController>();
            if (controller == null)
            {
                Scene.Add(controller = new PocketUmbrellaController());
            }
            prevVal = controller.Enabled;
            prevCost = controller.StaminaCost;
            prevCooldown = controller.Cooldown;
            if (Enable)
            {
                controller.Enabled = true;
                controller.StaminaCost = staminaCost;
                controller.Cooldown = cooldown;
                controller.MusicParam = musicParam;
            }
            else
            {
                Scene.Remove(controller);
            }
        }

        public override void OnLeave(Player player)
        {
            base.OnLeave(player);
            PocketUmbrellaController controller = Engine.Scene.Tracker.GetEntity<PocketUmbrellaController>();
            if (revertOnLeave && controller != null)
            {
                controller.StaminaCost = prevCost;
                controller.Cooldown = prevCooldown;
                if (prevVal)
                {
                    controller.Enabled = true;
                }
                else
                {
                    Scene.Remove(controller);
                }
            }
        }


    }
}
