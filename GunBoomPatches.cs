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

            // Получаем предмет напрямую из инвентаря для проверки качества
            byte page = equipment.equippedPage;
            byte x = equipment.equipped_x;
            byte y = equipment.equipped_y;
            byte index = player.inventory.getIndex(page, x, y);
            ItemJar jar = player.inventory.getItem(page, index);
            
            if (jar == null || jar.item == null) return true; 

            byte quality = jar.item.quality;
            if (quality > config.MinQuality) return true;

            float roll = UnityEngine.Random.value;

            // 1. ВЗРЫВ (Критическая поломка)
            if (config.EnableExplosion && roll < config.ExplosionChance)
            {
                EPlayerKill kill; // Объявляем заранее для совместимости с MSBuild
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

                    // Обнуляем данные о магазине в массиве state
                    state[8] = 0; 
                    state[9] = 0; 
                    state[13] = 0; 
                    state[14] = 0;
                    
                    equipment.sendUpdateState();
                    return false;
                }
            }

            // 3. ЗАКЛИНИВАНИЕ (Jam)
            if (config.EnableJam && roll < (config.ExplosionChance + config.MagDropChance + config.JamChance))
            {
                GunBoomPlugin.Instance.JammedPlayers.Add(sID);
                return false;
            }

            // 4. ЗАПАДАНИЕ СПУСКА (Runaway Gun)
            if (config.EnableRunaway && roll < (config.ExplosionChance + config.MagDropChance + config.JamChance + config.RunawayChance))
            {
                GunBoomPlugin.Instance.RunawayPlayers.Add(sID);
            }

            return true;
        }
    }

    // ПАТЧ 2: Эмуляция зажатой клавиши для Runaway Gun
    [HarmonyPatch(typeof(PlayerInput), "Update")]
    public class Patch_RunawayInput
    {
        [HarmonyPostfix]
        static void Postfix(PlayerInput __instance)
        {
            CSteamID sID = __instance.player.channel.owner.playerID.steamID;

            if (GunBoomPlugin.Instance.RunawayPlayers.Contains(sID))
            {
                var equip = __instance.player.equipment;
                // Стреляем только пока есть патроны и в руках огнестрел
                if (equip.asset is ItemGunAsset && BitConverter.ToUInt16(equip.state, 8) > 0)
                {
                    __instance.keys[0] = true; // Принудительно ставим флаг стрельбы
                }
                else
                {
                    GunBoomPlugin.Instance.RunawayPlayers.Remove(sID);
                }
            }
        }
    }

    // ПАТЧ 3: Сброс состояний при убирании предмета
    [HarmonyPatch(typeof(PlayerEquipment), "dequip")]
    public class Patch_EquipmentDequip
    {
        [HarmonyPostfix]
        static void Postfix(PlayerEquipment __instance)
        {
            CSteamID sID = __instance.player.channel.owner.playerID.steamID;
            GunBoomPlugin.Instance.RunawayPlayers.Remove(sID);
        }
    }
}
