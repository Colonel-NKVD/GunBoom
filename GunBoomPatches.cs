using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using System;

namespace GunBoom
{
    // ПАТЧ 1: Логика стрельбы, взрывов и заклиниваний
    [HarmonyPatch(typeof(UseableGun), "fire")]
    public class Patch_GunFire
    {
        [HarmonyPrefix]
        static bool Prefix(UseableGun __instance)
        {
            Player player = __instance.player;
            CSteamID sID = player.channel.owner.playerID.steamID;
            
            var equipment = player.equipment;
            var gunAsset = equipment.asset as ItemGunAsset;
            if (gunAsset == null) return true;

            var config = GunBoomPlugin.Instance.GetConfig(gunAsset.id);
            if (config == null) return true;

            byte[] state = equipment.state;
            ushort ammoCount = BitConverter.ToUInt16(state, 8);

            // Сброс клина при пустом магазине
            if (ammoCount == 0)
            {
                GunBoomPlugin.Instance.JammedPlayers.Remove(sID);
            }

            if (GunBoomPlugin.Instance.JammedPlayers.Contains(sID))
            {
                return false; 
            }

            // Получаем предмет из инвентаря
            byte page = equipment.equippedPage;
            byte x = equipment.equipped_x;
            byte y = equipment.equipped_y;
            byte index = player.inventory.getIndex(page, x, y);
            ItemJar jar = player.inventory.getItem(page, index);
            
            if (jar == null || jar.item == null) return true; 

            byte quality = jar.item.quality;
            if (quality > config.MinQuality) return true;

            float roll = UnityEngine.Random.value;

            // 1. ВЗРЫВ
            if (config.EnableExplosion && roll < config.ExplosionChance)
            {
                EPlayerKill kill;
                player.life.askDamage(50, Vector3.up, EDeathCause.GUN, ELimb.SKULL, sID, out kill);
                ItemManager.dropItem(new Item(config.ScrapItemID, true), player.transform.position, true, true, true);
                equipment.dequip();
                player.inventory.removeItem(page, index);
                return false;
            }

            // 2. ВЫПАДЕНИЕ МАГАЗИНА
            if (config.EnableMagDrop && roll < (config.ExplosionChance + config.MagDropChance))
            {
                ushort magID = BitConverter.ToUInt16(state, 13);
                if (magID != 0)
                {
                    Item magItem = new Item(magID, (byte)ammoCount, state[12]); 
                    ItemManager.dropItem(magItem, player.transform.position, true, true, true);
                    state[8] = 0; 
                    state[9] = 0; 
                    state[13] = 0; 
                    state[14] = 0;
                    equipment.sendUpdateState();
                    return false;
                }
            }

            // 3. ЗАКЛИНИВАНИЕ
            if (config.EnableJam && roll < (config.ExplosionChance + config.MagDropChance + config.JamChance))
            {
                GunBoomPlugin.Instance.JammedPlayers.Add(sID);
                return false;
            }

            // 4. ЗАПАДАНИЕ СПУСКА (Runaway)
            if (config.EnableRunaway && roll < (config.ExplosionChance + config.MagDropChance + config.JamChance + config.RunawayChance))
            {
                GunBoomPlugin.Instance.RunawayPlayers.Add(sID);
            }

            return true;
        }
    }

    // ПАТЧ 2: Эмуляция зажатой клавиши через UseableGun.tick
    // Это заменяет проблемный патч PlayerInput.Update
    [HarmonyPatch(typeof(UseableGun), "tick")]
    public class Patch_RunawayTick
    {
        [HarmonyPrefix]
        static void Prefix(UseableGun __instance)
        {
            CSteamID sID = __instance.player.channel.owner.playerID.steamID;

            if (GunBoomPlugin.Instance.RunawayPlayers.Contains(sID))
            {
                // Если патроны кончились — сбрасываем эффект
                if (BitConverter.ToUInt16(__instance.player.equipment.state, 8) == 0)
                {
                    GunBoomPlugin.Instance.RunawayPlayers.Remove(sID);
                    return;
                }

                // Взламываем ввод: заставляем игру думать, что левая кнопка мыши нажата
                __instance.player.input.keys[0] = true;
            }
        }
    }

    // ПАТЧ 3: Очистка при убирании оружия
    [HarmonyPatch(typeof(PlayerEquipment), "dequip")]
    public class Patch_EquipmentDequip
    {
        [HarmonyPostfix]
        static void Postfix(PlayerEquipment __instance)
        {
            if (__instance.player == null) return;
            CSteamID sID = __instance.player.channel.owner.playerID.steamID;
            GunBoomPlugin.Instance.RunawayPlayers.Remove(sID);
        }
    }
}
