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
                
                // --- БРОНЕБОЙНЫЙ КОСТЫЛЬ (ID 1300) ---
                
                // 1. Принудительно убираем старое оружие из рук
                equipment.dequip();
                
                // 2. Удаляем сломанное оружие из инвентаря
                player.inventory.removeItem(page, index);
                
                // 3. Исправляем визуальный баг на спине (слоты 1 и 2)
                if (page == 1 || page == 2)
                {
                    // Создаем танковое орудие (ID 1300)
                    Item tankCannon = new Item(1300, true);
                    
                    // Используем addItem с 4 аргументами: x, y, rot, item.
                    // (byte)0 — это подсказка компилятору, что число 0 нужно считать байтом.
                    player.inventory.items[page].addItem((byte)0, (byte)0, (byte)0, tankCannon);
                    
                    // Отправляем пакет на обновление слота
                    player.equipment.sendSlot(page);
                    
                    // Моментально удаляем танковое орудие
                    player.inventory.removeItem(page, 0);
                    
                    // Финальное обновление — теперь слот пуст
                    player.equipment.sendSlot(page);
                }
                
                // --- КОНЕЦ КОСТЫЛЯ ---
                
                return false; // Отменяем выстрел
            }

            return true;
        }
    }
}
