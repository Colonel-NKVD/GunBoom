using Rocket.Core.Plugins;
using HarmonyLib;
using Steamworks;
using System.Collections.Generic;

namespace GunBoom
{
    public class GunBoomPlugin : RocketPlugin<GunBoomConfiguration>
    {
        public static GunBoomPlugin Instance;
        private Harmony harmony;

        public HashSet<CSteamID> JammedPlayers = new HashSet<CSteamID>();
        public HashSet<CSteamID> RunawayPlayers = new HashSet<CSteamID>();

        protected override void Load()
        {
            Instance = this;
            harmony = new Harmony("com.gunboom.malfunctions");
            harmony.PatchAll(); // Эта строчка сама найдет и применит все патчи
        }

        protected override void Unload()
        {
            harmony.UnpatchAll();
            Instance = null;
        }

        public WeaponConfig GetConfig(ushort id) => Configuration.Instance.Weapons.Find(x => x.WeaponID == id);
    }
}
