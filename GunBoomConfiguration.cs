using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

public class WeaponConfig
{
    [XmlAttribute("WeaponID")]
    public ushort WeaponID;

    [XmlAttribute("MinQuality")]
    public byte MinQuality; // Порог прочности (например, 30%), ниже которого начинаются проблемы

    // Выпадение магазина
    public bool EnableMagDrop;
    public float MagDropChance;

    // Взрыв оружия
    public bool EnableExplosion;
    public float ExplosionChance;
    public ushort ExplosionSpawnItemID; // ID предмета (например, металлолома), который появится

    // Западание спуска (Runaway Gun)
    public bool EnableRunaway;
    public float RunawayChance;

    // Заклинивание (Jamming)
    public bool EnableJam;
    public float JamChance;
}

public class GunBoomConfiguration : IRocketPluginConfiguration
{
    [XmlArrayItem(ElementName = "Weapon")]
    public List<WeaponConfig> Weapons;

    public void LoadDefaults()
    {
        Weapons = new List<WeaponConfig>
        {
            new WeaponConfig
            {
                WeaponID = 363, // Пример: Maplestrike
                MinQuality = 25,
                EnableMagDrop = true,
                MagDropChance = 0.05f, // 5%
                EnableExplosion = true,
                ExplosionChance = 0.01f, // 1%
                ExplosionSpawnItemID = 67, // Металлолом
                EnableRunaway = true,
                RunawayChance = 0.02f, // 2%
                EnableJam = true,
                JamChance = 0.1f // 10%
            }
        };
    }
}
