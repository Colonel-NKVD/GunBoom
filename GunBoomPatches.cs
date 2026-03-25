using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using System;

namespace GunBoom
{
    [HarmonyPatch]
    public class GunBoomPatches
    {
        // --- ЛОГИКА СТРЕЛЬБЫ ---
        [HarmonyPatch(typeof(UseableGun), "fire")]
        [HarmonyPrefix]
        static bool GunFirePrefix(UseableGun __instance)
        {
            Player player = __instance.player;
            CSteamID sID = player.channel.owner.playerID.steamID;
            
            // 1. Проверка на клин (блокируем выстрел)
            if (GunBoomPlugin.Instance.JammedPlayers.Contains(sID))
            {
                // Можно добавить звук "осечки" здесь
                return false; 
            }

            var equipment = player.equipment;
            var gunAsset = equipment.asset as ItemGunAsset;
            if (gunAsset == null) return true;

            var config = GunBoomPlugin.Instance.GetConfig(gunAsset.id);
            if (config == null) return true;

            // 2. Получаем данные о предмете
            byte page = equipment.equippedPage;
            byte x = equipment.equipped_x;
            byte y = equipment.equipped_y;
            byte index = player.inventory.getIndex(page, x, y);
            ItemJar jar = player.inventory.getItem(page, index);
            
            if (jar == null) return true;
            if (jar.item.quality > config.MinQuality) return true;

            float roll = UnityEngine.Random.value;
            byte[] state = equipment.state;
            ushort ammo = BitConverter.ToUInt16(state, 8);

            // 3. РАНДОМ ПОЛОМОК
            // Взрыв (приоритет 1)
            if (config.EnableExplosion && roll < config.ExplosionChance)
            {
                EPlayerKill kill;
                player.life.askDamage(50, Vector3.up, EDeathCause.GUN, ELimb.SKULL, sID, out kill);
                ItemManager.dropItem(new Item(config.ScrapItemID, true), player.transform.position, true, true, true);
                
                equipment.dequip();
                player.inventory.removeItem(page, index);
                return false;
            }

            // Выпадение магазина (приоритет 2)
            if (config.EnableMagDrop && roll < (config.ExplosionChance + config.MagDropChance))
            {
                ushort magID = BitConverter.ToUInt16(state, 13);
                if (magID != 0)
                {
                    Item magItem = new Item(magID, (byte)ammo, state[12]);
                    ItemManager.dropItem(magItem, player.transform.position, true, true, true);
                    
                    state[8] = 0; state[9] = 0; // Патроны в 0
                    state[13] = 0; state[14] = 0; // Магазин удален
                    equipment.sendUpdateState();
                }
                return false;
            }

            // Заклинивание (приоритет 3)
            if (config.EnableJam && roll < (config.ExplosionChance + config.MagDropChance + config.JamChance))
            {
                GunBoomPlugin.Instance.JammedPlayers.Add(sID);
                return false;
            }

            // Западание спуска (приоритет 4)
            if (config.EnableRunaway && roll < (config.ExplosionChance + config.MagDropChance + config.JamChance + config.RunawayChance))
            {
                GunBoomPlugin.Instance.RunawayPlayers.Add(sID);
            }

            return true;
        }

        // --- ИСПРАВЛЕНИЕ ЗАПАДАНИЯ (RUNAWAY) ---
        [HarmonyPatch(typeof(UseableGun), "tick")]
        [HarmonyPrefix]
        static void GunTickPrefix(UseableGun __instance)
        {
            CSteamID sID = __instance.player.channel.owner.playerID.steamID;
            if (GunBoomPlugin.Instance.RunawayPlayers.Contains(sID))
            {
                // Если магазин пуст, прекращаем автоматическую стрельбу
                if (BitConverter.ToUInt16(__instance.player.equipment.state, 8) == 0)
                {
                    GunBoomPlugin.Instance.RunawayPlayers.Remove(sID);
                    return;
                }
                // Симулируем нажатие ЛКМ
                __instance.player.input.keys[0] = true;
            }
        }

        // --- ОЧИСТКА КЛИНА ПРИ ПЕРЕЗАРЯДКЕ ---
        [HarmonyPatch(typeof(UseableGun), "askReload")]
        [HarmonyPrefix]
        static void GunReloadPrefix(UseableGun __instance)
        {
            CSteamID sID = __instance.player.channel.owner.playerID.steamID;
            // Перезарядка — лучший способ прочистить заклинивший механизм
            if (GunBoomPlugin.Instance.JammedPlayers.Contains(sID))
            {
                GunBoomPlugin.Instance.JammedPlayers.Remove(sID);
            }
        }

        // --- СБРОС ПРИ СМЕНЕ ОРУЖИЯ ---
        [HarmonyPatch(typeof(PlayerEquipment), "dequip")]
        [HarmonyPostfix]
        static void DequipPostfix(PlayerEquipment __instance)
        {
            if (__instance.player == null) return;
            CSteamID sID = __instance.player.channel.owner.playerID.steamID;
            GunBoomPlugin.Instance.RunawayPlayers.Remove(sID);
            // Клин НЕ удаляем, чтобы игрок мучился, пока не перезарядит его
        }
    }
}
