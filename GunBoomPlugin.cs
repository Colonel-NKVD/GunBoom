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
        
        public HashSet<CSteamID> JammedPlayers = new HashSet<CSteamID>();
        public HashSet<CSteamID> RunawayPlayers = new HashSet<CSteamID>();

        protected override void Load()
        {
            Instance = this;
            harmony = new Harmony("com.ironandmud.gunboom");
            harmony.PatchAll();
            
            // ИСПРАВЛЕНО: Используем Provider.onEnemyDisconnected вместо Player
            Provider.onEnemyDisconnected += OnPlayerDisconnected;
            
            Rocket.Core.Logging.Logger.Log("GunBoom: Weapon malfunctions initialized.");
        }

        protected override void Unload()
        {
            Provider.onEnemyDisconnected -= OnPlayerDisconnected;
            harmony.UnpatchAll();
            JammedPlayers.Clear();
            RunawayPlayers.Clear();
        }

        // Аргумент изменен на SteamPlayer для соответствия событию
        private void OnPlayerDisconnected(SteamPlayer player)
        {
            if (player == null) return;
            CSteamID sID = player.playerID.steamID;
            JammedPlayers.Remove(sID);
            RunawayPlayers.Remove(sID);
        }

        public WeaponConfig GetConfig(ushort id)
        {
            return Configuration.Instance.Weapons.Find(x => x.WeaponID == id);
        }
    }
}
