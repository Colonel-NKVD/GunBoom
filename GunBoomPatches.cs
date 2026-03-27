using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using System;

namespace GunBoom
{
    [HarmonyPatch(typeof(UseableGun), "fire")]
    public class Patch_GunFire
    {
        [HarmonyPrefix]
        static bool Prefix(UseableGun __instance)
        {
            Player player = __instance.player;
            var equipment = player.equipment;
            var gunAsset = equipment.asset as ItemGunAsset;
            
            if (gunAsset == null) return true;

            var config = GunBoomPlugin.Instance.GetConfig(gunAsset.id);
            if (config == null) return true;

            byte page = equipment.equippedPage;
            byte x = equipment.equipped_x;
            byte y = equipment.equipped_y;
            byte index = player.inventory.getIndex(page, x, y);
            ItemJar jar = player.inventory.getItem(page, index);
            
            if (jar == null || jar.item == null) return true; 

            if (jar.item.quality > config.MinQuality) return true;

            float roll = UnityEngine.Random.value;

            if (roll < config.ExplosionChance)
            {
                CSteamID sID = player.channel.owner.playerID.steamID;

                // Эффект взрыва
                Vector3 explosionPosition = player.transform.position + new Vector3(0f, 1.2f, 0f);
                EffectManager.sendEffect(54, 80, explosionPosition);
                
                // Урон игроку
                EPlayerKill kill; 
                player.life.askDamage(50, Vector3.up, EDeathCause.GUN, ELimb.SKULL, sID, out kill);
                
                // Спавн металлолома
                ItemManager.dropItem(new Item(config.ScrapItemID, true), player.transform.position, true, true, true);
                
                // --- НАЧАЛО БРОНЕБОЙНОГО КОСТЫЛЯ (ID 1300) ---
                
                // 1. Принудительно убираем старое оружие из рук
                equipment.dequip();
                
                // 2. Удаляем сломанное оружие из инвентаря
                player.inventory.removeItem(page, index);
                
                // 3. Проверяем, было ли оружие в основных слотах (1 - Primary, 2 - Secondary)
                if (page == 1 || page == 2)
                {
                    // Берем танковое орудие (ID 1300)
                    Item tankCannon = new Item(1300, true);
                    
                    // Запихиваем танковый ствол в слот на спине
                    player.inventory.items[page].tryAddItem(tankCannon, 0, 0, 0, true);
                    
                    // Заставляем клиент перерисовать спину (клиент вынужден отрендерить пушку танка)
                    player.equipment.sendSlot(page);
                    
                    // Моментально удаляем танковый ствол
                    player.inventory.removeItem(page, 0);
                    
                    // Финальный пакет: слот гарантированно пуст
                    player.equipment.sendSlot(page);
                }
                
                // --- КОНЕЦ КОСТЫЛЯ ---
                
                return false; 
            }

            return true;
        }
    }
}
