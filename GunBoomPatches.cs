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
            
            // Если в руках не огнестрел — игнорируем
            if (gunAsset == null) return true;

            // Ищем конфиг для этого ID оружия
            var config = GunBoomPlugin.Instance.GetConfig(gunAsset.id);
            if (config == null) return true;

            // Получаем предмет из инвентаря для проверки качества
            byte page = equipment.equippedPage;
            byte x = equipment.equipped_x;
            byte y = equipment.equipped_y;
            byte index = player.inventory.getIndex(page, x, y);
            ItemJar jar = player.inventory.getItem(page, index);
            
            // Защита от Null (если предмет исчез в момент выстрела)
            if (jar == null || jar.item == null) return true; 

            // Если качество выше опасного порога — стреляем штатно
            if (jar.item.quality > config.MinQuality) return true;

            float roll = UnityEngine.Random.value;

            // Проверка шанса взрыва
            if (roll < config.ExplosionChance)
            {
                CSteamID sID = player.channel.owner.playerID.steamID;

                // --- НОВОЕ: Спавн визуального эффекта №54 ---
                // Расчитываем позицию на уровне рук/груди (стандартная высота игрока ~1.8м)
                // player.transform.position находится в ногах, добавляем ~1.2м по вертикали.
                Vector3 explosionPosition = player.transform.position + new Vector3(0f, 1.2f, 0f);
                
                // Спавним эффект (стандартный взрыв гранаты)
                // 1.0f - это радиус/скейл эффекта, Vector3.up - направление.
                EffectManager.sendEffect(54, 1.0f, explosionPosition, Vector3.up);
                // ------------------------------------------
                
                // Наносим урон игроку (вынесено для старых компиляторов CI/CD)
                EPlayerKill kill; 
                player.life.askDamage(50, Vector3.up, EDeathCause.GUN, ELimb.SKULL, sID, out kill);
                
                // Спавним предмет-хлам на месте игрока
                ItemManager.dropItem(new Item(config.ScrapItemID, true), player.transform.position, true, true, true);
                
                // Принудительно убираем оружие из рук и удаляем его
                equipment.dequip();
                player.inventory.removeItem(page, index);
                
                return false; // Отменяем сам выстрел
            }

            return true;
        }
    }
}
