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

            // ПРОВЕРКА КЛИНА: Если патронов 0, клин снимается
            if (ammoCount == 0)
            {
                GunBoomPlugin.Instance.JammedPlayers.Remove(sID);
            }

            if (GunBoomPlugin.Instance.JammedPlayers.Contains(sID))
            {
                return false; 
            }

            // --- ПРАВИЛЬНОЕ ИЗВЛЕЧЕНИЕ ПРЕДМЕТА ИЗ ИНВЕНТАРЯ ---
            byte page = equipment.equippedPage;
            byte x = equipment.equipped_x;
            byte y = equipment.equipped_y;
            
            byte index = player.inventory.getIndex(page, x, y);
            ItemJar jar = player.inventory.getItem(page, index);
            
            // Защита от NullReference
            if (jar == null) return true; 

            byte quality = jar.item.quality;

            // Если прочность в норме — выходим
            if (quality > config.MinQuality) return true;

            float roll = UnityEngine.Random.value;

            // 1. ВЗРЫВ
            if (config.EnableExplosion && roll < config.ExplosionChance)
            {
                player.life.askDamage(50, Vector3.up, EDeathCause.GUN, ELimb.SKULL, sID, out EPlayerKill kill);
                
                // Спавним лом
                ItemManager.dropItem(new Item(config.ScrapItemID, true), player.transform.position, true, true, true);
                
                // Правильное удаление предмета из рук и инвентаря
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

                    // Очищаем данные о магазине в пушке
                    state[8] = 0; 
                    state[9] = 0; 
                    state[13] = 0; 
                    state[14] = 0;
                    
                    // Правильная отправка обновленного state клиентам
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

            // 4. ЗАПАДАНИЕ СПУСКА
            if (config.EnableRunaway && roll < (config.ExplosionChance + config.
