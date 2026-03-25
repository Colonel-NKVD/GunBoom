using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GunBoom
{
    public class GunBoomConfiguration : IRocketPluginConfiguration
    {
        public string LoadMessage;
        public List<WeaponConfig> Weapons;

        public void Defaults()
        {
            LoadMessage = "GunBoom Malfunctions Loaded!";
            Weapons = new List<WeaponConfig>
            {
                new WeaponConfig
                {
                    WeaponID = 122, // Zubeknakov
                    MinQuality = 15,
                    
                    EnableExplosion = true,
                    ExplosionChance = 0.01f,
                    ScrapItemID = 67, // Metal Scrap
                    
                    EnableMagDrop = true,
                    MagDropChance = 0.05f,
                    
                    EnableJam = true,
                    JamChance = 0.10f,
                    
                    EnableRunaway = true,
                    RunawayChance = 0.03f
                }
            };
        }
    }

    public class WeaponConfig
    {
        [XmlAttribute("ID")]
        public ushort WeaponID;
        
        [XmlAttribute("MinQual")]
        public byte MinQuality;

        // Взрыв
        public bool EnableExplosion;
        public float ExplosionChance;
        public ushort ScrapItemID; 

        // Выпадение магазина
        public bool EnableMagDrop;
        public float MagDropChance;

        // Заклинивание
        public bool EnableJam;
        public float JamChance;

        // Западание спуска
        public bool EnableRunaway;
        public float RunawayChance;
    }
}
