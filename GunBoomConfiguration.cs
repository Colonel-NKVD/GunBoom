using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GunBoom
{
    public class GunBoomConfiguration : IRocketPluginConfiguration
    {
        public string LoadMessage;
        public List<WeaponConfig> Weapons;

        // ИСПРАВЛЕНО: RocketMod ищет именно LoadDefaults()
        public void LoadDefaults()
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

        public bool EnableExplosion;
        public float ExplosionChance;
        public ushort ScrapItemID; 

        public bool EnableMagDrop;
        public float MagDropChance;

        public bool EnableJam;
        public float JamChance;

        public bool EnableRunaway;
        public float RunawayChance;
    }
}
