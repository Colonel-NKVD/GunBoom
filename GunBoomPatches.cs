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
                
                // --- ИСПРАВЛЕНИЕ БАГА ---
                // Мы убрали equipment.dequip();
                // Просто удаляем предмет. Unturned сам поймет, что предмет был в руках,
                // уберет его анимацию и корректно очистит инвентарь без создания "призраков".
                player.inventory.removeItem(page, index);
                
                // Контрольный выстрел: принудительно говорим всем клиентам вокруг, 
                // что слот на спине (1 - основной, 2 - вторичный) теперь пуст.
                if (page == 1 || page == 2)
                {
                    player.equipment.sendSlot(page);
                }
                // ------------------------
                
                return false; 
            }

            return true;
        }
    }
}
