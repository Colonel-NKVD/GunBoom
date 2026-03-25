using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using System;

namespace GunBoom
{
    // ПАТЧ 1: Логика стрельбы и поломок
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

            // ПРОВЕРКА КЛИНА: Если патронов 0, клин снимается автоматически (магазин вынут)
            if (ammoCount == 0)
            {
                GunBoomPlugin.Instance.JammedPlayers.Remove(sID);
            }

            if (GunBoomPlugin.Instance.JammedPlayers.Contains(sID))
            {
                // Оружие заклинило — выстрела нет
                return false; 
            }

            // Если прочность в норме — выходим
            if (equipment.item.quality > config.MinQuality) return true;

            float roll = UnityEngine.Random.value;

            // 1. ВЗРЫВ
            if (config.EnableExplosion && roll < config.ExplosionChance)
            {
                player.life.askDamage(50, Vector3.up, EDeathCause.GUN, ELimb.SKULL, sID, out EPlayerKill kill);
                ItemManager.dropItem(new Item(config.ScrapItemID, true), player.transform.position, true, true, true);
                
                player.inventory.removeItem(equipment.equippable_x, equipment.equippable_y);
                return false;
            }

            // 2. ВЫПАДЕНИЕ МАГАЗИНА (с сохранением патронов)
            if (config.EnableMagDrop && roll < (config.ExplosionChance + config.MagDropChance))
            {
                ushort magID = BitConverter.ToUInt16(state, 13);
                if (magID != 0)
                {
                    // Создаем новый предмет-магазин. 
                    // В Unturned для магазинов количество (amount) — это и есть патроны.
                    Item magItem = new Item(magID, (byte)ammoCount, state[12]); 
                    ItemManager.dropItem(magItem, player.transform.position, true, true, true);

                    // Очищаем данные о магазине в пушке
                    state[8] = 0; // Ammo L
                    state[9] = 0; // Ammo H
                    state[13] = 0; // Mag ID L
                    state[14] = 0; // Mag ID H
                    
                    equipment.updateState(state);
                    return false;
                }
            }

            // 3. ЗАКЛИНИВАНИЕ
            if (config.EnableJam && roll < (config.ExplosionChance + config.MagDropChance + config.JamChance))
            {
                GunBoomPlugin.Instance.JammedPlayers.Add(sID);
                return false;
            }

            // 4. ЗАПАДАНИЕ СПУСКА
            if (config.EnableRunaway && roll < (config.ExplosionChance + config.MagDropChance + config.JamChance + config.RunawayChance))
            {
                GunBoomPlugin.Instance.RunawayPlayers.Add(sID);
            }

            return true;
        }
    }

    // ПАТЧ 2: Принудительная стрельба (Runaway Gun)
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
                
                // Проверяем: в руках ли всё еще пушка и есть ли в ней патроны?
                if (equip.asset is ItemGunAsset && BitConverter.ToUInt16(equip.state, 8) > 0)
                {
                    // Имитируем зажатую ЛКМ (индекс 0 в массиве клавиш)
                    __instance.keys[0] = true;
                }
                else
                {
                    // Если патроны кончились или пушку убрали — эффект проходит
                    GunBoomPlugin.Instance.RunawayPlayers.Remove(sID);
                }
            }
        }
    }
}
