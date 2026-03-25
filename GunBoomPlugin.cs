using Rocket.Core.Plugins;
using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

public class GunBoomPlugin : RocketPlugin<GunBoomConfiguration>
{
    public static GunBoomPlugin Instance;
    public const string HarmonyInstanceId = "com.gunboom.plugin";
    private Harmony harmony;

    // Храним SteamID игроков, у которых заклинило оружие
    public HashSet<CSteamID> JammedPlayers = new HashSet<CSteamID>();
    
    // Храним SteamID игроков, у которых запал спуск
    public HashSet<CSteamID> RunawayPlayers = new HashSet<CSteamID>();

    protected override void Load()
    {
        Instance = this;
        harmony = new Harmony(HarmonyInstanceId);
        harmony.PatchAll();
        
        // Подписываемся на смену предмета, чтобы сбрасывать состояния, если игрок убрал оружие
        PlayerEquipment.OnEquipRequested_Global += OnEquipRequested;
    }

    protected override void Unload()
    {
        harmony.UnpatchAll(HarmonyInstanceId);
        PlayerEquipment.OnEquipRequested_Global -= OnEquipRequested;
        JammedPlayers.Clear();
        RunawayPlayers.Clear();
        Instance = null;
    }

    private void OnEquipRequested(EquipmentRequestHandler handler)
    {
        // Если игрок меняет оружие, сбрасываем баг с западанием спуска
        var steamId = handler.equipment.player.channel.owner.playerID.steamID;
        RunawayPlayers.Remove(steamId);
    }
    
    public WeaponConfig GetConfigForWeapon(ushort id)
    {
        return Configuration.Instance.Weapons.Find(w => w.WeaponID == id);
    }
}
