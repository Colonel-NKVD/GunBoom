using Rocket.Core.Plugins;
using HarmonyLib;
using Steamworks;
using System.Collections.Generic;
using SDG.Unturned;

namespace GunBoom
{
    public class GunBoomPlugin : RocketPlugin<GunBoomConfiguration>
    {
        public static GunBoomPlugin Instance;
        private Harmony harmony;
        
        // Используем HashSet для быстрой проверки состояний
        public HashSet<CSteamID> JammedPlayers = new HashSet<CSteamID>();
        public HashSet<CSteamID> RunawayPlayers = new HashSet<CSteamID>();

        protected override void Load()
        {
            Instance = this;
            harmony = new Harmony("com.ironandmud.gunboom");
            harmony.PatchAll();
            
            Player.onPlayerDisconnected += OnPlayerDisconnected;
            Rocket.Core.Logging.Logger.Log("GunBoom Ready for 1917+ warfare.");
        }

        protected override void Unload()
        {
            Player.onPlayerDisconnected -= OnPlayerDisconnected;
            harmony.UnpatchAll();
            JammedPlayers.Clear();
            RunawayPlayers.Clear();
        }

        private void OnPlayerDisconnected(Player player)
        {
            JammedPlayers.Remove(player.channel.owner.playerID.steamID);
            RunawayPlayers.Remove(player.channel.owner.playerID.steamID);
        }

        public WeaponConfig GetConfig(ushort id)
        {
            return Configuration.Instance.Weapons.Find(x => x.WeaponID == id);
        }
    }
}
