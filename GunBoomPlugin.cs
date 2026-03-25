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

        // Состояния игроков
        public HashSet<CSteamID> JammedPlayers = new HashSet<CSteamID>();
        public HashSet<CSteamID> RunawayPlayers = new HashSet<CSteamID>();

        protected override void Load()
        {
            Instance = this;
            harmony = new Harmony("com.gunboom.malfunctions");
            harmony.PatchAll();
            
            PlayerEquipment.OnEquipRequested_Global += OnEquip;
        }

        protected override void Unload()
        {
            PlayerEquipment.OnEquipRequested_Global -= OnEquip;
            harmony.UnpatchAll();
            Instance = null;
        }

        private void OnEquip(EquipmentRequestHandler handler)
        {
            // Сброс западания спуска при смене оружия
            RunawayPlayers.Remove(handler.equipment.player.channel.owner.playerID.steamID);
        }

        public WeaponConfig GetConfig(ushort id) => Configuration.Instance.Weapons.Find(x => x.WeaponID == id);
    }
}
