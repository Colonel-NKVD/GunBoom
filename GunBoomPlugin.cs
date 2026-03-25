using Rocket.Core.Plugins;
using HarmonyLib;

namespace GunBoom
{
    public class GunBoomPlugin : RocketPlugin<GunBoomConfiguration>
    {
        public static GunBoomPlugin Instance;
        private Harmony harmony;

        protected override void Load()
        {
            Instance = this;
            harmony = new Harmony("com.ironandmud.gunboom");
            harmony.PatchAll();
            
            Rocket.Core.Logging.Logger.Log("GunBoom: Explosions only. Welcome to the trenches.");
        }

        protected override void Unload()
        {
            harmony.UnpatchAll("com.ironandmud.gunboom");
        }

        public WeaponConfig GetConfig(ushort id)
        {
            return Configuration.Instance.Weapons?.Find(x => x.WeaponID == id);
        }
    }
}
