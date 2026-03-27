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
                
                // --- РАБОЧИЙ КОСТЫЛЬ ЧЕРЕЗ TIMBERWOLF (ID 18) ---
                
                // 1. Сначала убираем пушку из рук (в кобуру/на спину)
                equipment.dequip();
                
                // 2. Удаляем сломанный калаш
                player.inventory.removeItem(page, index);
                
                // 3. Сразу говорим клиентам: "Тут пусто!"
                player.equipment.sendSlot(page);

                // 4. Проверяем слоты на спине (1 и 2)
                if (page == 1 || page == 2)
                {
                    // Создаем Timberwolf (ID 18) — он гарантированно перекроет любую модельку на спине
                    Item ghostCleaner = new Item(18, true);
                    
                    // Силой запихиваем снайперку в слот
                    player.inventory.items[page].addItem((byte)0, (byte)0, (byte)0, ghostCleaner);
                    
                    // Рассылаем пакет: "Смотрите, теперь тут снайперка!" 
                    // Это заставит Unity уничтожить старую модельку калаша и создать новую.
                    player.equipment.sendSlot(page);
                    
                    // Тут же удаляем снайперку
                    player.inventory.removeItem(page, 0);
                    
                    // Финальный пакет: "Теперь тут точно пусто"
                    player.equipment.sendSlot(page);
                }
                
                // --- КОНЕЦ КОСТЫЛЯ ---
                
                return false; // Отменяем выстрел, чтобы калаш не "стрельнул" перед исчезновением
            }

            return true;
        }
    }
}
