using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GunBoom
{
    public class GunBoomConfiguration : IRocketPluginConfiguration
    {
        public string LoadMessage;
        public List<WeaponConfig> Weapons;

        public void LoadDefaults()
        {
            LoadMessage = "GunBoom: Simplified Explosion Mode Loaded!";
            Weapons = new List<WeaponConfig>
            {
                new WeaponConfig
                {
                    WeaponID = 122, // ID оружия
                    MinQuality = 15, // Порог износа, ниже которого возможен взрыв
                    ExplosionChance = 0.05f, // Шанс 5%
                    ScrapItemID = 67 // ID выпадающего предмета (например, Metal Scrap)
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

        public float ExplosionChance;
        public ushort ScrapItemID; 
    }
}
