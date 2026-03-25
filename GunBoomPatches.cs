using HarmonyLib;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

[HarmonyPatch(typeof(UseableGun), "fire")]
public class UseableGun_Fire_Patch
{
    static bool Prefix(UseableGun __instance)
    {
        Player player = __instance.player;
        CSteamID steamId = player.channel.owner.playerID.steamID;
        
        var equipment = player.equipment;
        ItemGunAsset gunAsset = equipment.asset as ItemGunAsset;
        if (gunAsset == null) return true;

        // Проверяем, есть ли оружие в нашем конфиге
        var config = GunBoomPlugin.Instance.GetConfigForWeapon(gunAsset.id);
        if (config == null) return true;

        byte quality = equipment.item.quality;
        byte[] state = equipment.state;
        
        // Получаем текущее количество патронов (индексы 8 и 9 хранят ushort)
        ushort currentAmmo = System.BitConverter.ToUInt16(state, 8);

        // --- ЛОГИКА РАЗРЯДКИ (Починка заклинивания) ---
        // Если патронов 0 (магазин вытащили/разрядили), снимаем клин
        if (currentAmmo == 0)
        {
            GunBoomPlugin.Instance.JammedPlayers.Remove(steamId);
        }

        // Если оружие заклинило, стрелять нельзя
        if (GunBoomPlugin.Instance.JammedPlayers.Contains(steamId))
        {
            // Можно добавить звук щелчка (осечки) здесь
            return false; 
        }

        // Если прочность выше минимальной — всё работает штатно
        if (quality > config.MinQuality) return true;

        // --- ГЕНЕРАЦИЯ ПОЛОМОК ---
        float roll = Random.value;

        // 1. Взрыв оружия
        if (config.EnableExplosion && roll < config.ExplosionChance)
        {
            // Наносим урон игроку (например, 40 хп)
            player.life.askDamage(40, Vector3.up, EDeathCause.GUN, ELimb.SKULL, steamId, out EPlayerKill kill);
            
            // Спавним предмет на месте взрыва
            ItemManager.dropItem(new Item(config.ExplosionSpawnItemID, true), player.transform.position, true, true, true);
            
            // Уничтожаем оружие в руках
            equipment.dequip();
            player.inventory.removeItem(equipment.equippable_x, equipment.equippable_y);
            
            return false; // Отменяем текущий выстрел
        }

        // 2. Выпадение магазина
        if (config.EnableMagDrop && roll < config.ExplosionChance + config.MagDropChance)
        {
            ushort magId = System.BitConverter.ToUInt16(state, 13);
            if (magId != 0 && currentAmmo > 0)
            {
                // Создаем предмет-магазин с текущим количеством патронов внутри
                Item droppedMag = new Item(magId, 1, quality, state); // state магазина нужно будет пересобрать в идеале
                ItemManager.dropItem(droppedMag, player.transform.position, true, true, true);

                // Обнуляем патроны и ID магазина в state самого оружия
                state[8] = 0;
                state[9] = 0;
                state[13] = 0;
                state[14] = 0;
                
                equipment.updateState(state); // Обновляем состояние пушки
                return false;
            }
        }

        // 3. Заклинивание (Jamming)
        if (config.EnableJam && roll < config.ExplosionChance + config.MagDropChance + config.JamChance)
        {
            GunBoomPlugin.Instance.JammedPlayers.Add(steamId);
            return false; // Выстрел срывается
        }

        // 4. Западание спускового крючка
        if (config.EnableRunaway && roll < config.ExplosionChance + config.MagDropChance + config.JamChance + config.RunawayChance)
        {
            GunBoomPlugin.Instance.RunawayPlayers.Add(steamId);
            // Выстрел проходит, логика западания обрабатывается в Update
        }

        return true;
    }
}
