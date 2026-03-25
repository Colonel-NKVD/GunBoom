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

            // Находим предмет в инвентаре для проверки его качества
            byte page = equipment.equippedPage;
            byte x = equipment.equipped_x;
            byte y = equipment.equipped_y;
            byte index = player.inventory.getIndex(page, x, y);
            ItemJar jar = player.inventory.getItem(page, index);
            
            if (jar == null || jar.item == null) return true; 

            // Если качество выше опасного порога — стреляем штатно
            if (jar.item.quality > config.MinQuality) return true;

            float roll = UnityEngine.Random.value;

            // Проверка шанса взрыва
            if (roll < config.ExplosionChance)
            {
                CSteamID sID = player.channel.owner.playerID.steamID;
                
                // Наносим урон игроку (переменная вынесена для старых компиляторов)
                EPlayerKill kill; 
                player.life.askDamage(50, Vector3.up, EDeathCause.GUN, ELimb.SKULL, sID, out kill);
                
                // Спавним металлолом или запчасти на месте игрока
                ItemManager.dropItem(new Item(config.ScrapItemID, true), player.transform.position, true, true, true);
                
                // Убираем оружие из рук и удаляем из инвентаря
                equipment.dequip();
                player.inventory.removeItem(page, index);
                
                return false; // Отменяем сам выстрел
            }

            return true;
        }
    }
}
