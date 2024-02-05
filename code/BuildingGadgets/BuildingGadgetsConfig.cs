using System;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;
using Newtonsoft.Json;

namespace BuildingGadgets
{
    public class BuildingGadgetsConfig
    {
        // config entries
        /// <summary>
        /// Use Durability on the gadget, requires repair/replacement, default true;
        /// </summary>
        public bool useDurability;
        /// <summary>
        /// Use Construction Paste instead of actual blocks...
        /// </summary>
        public bool usePaste;
        /// <summary>
        /// Max range of building gadget, default 5 (11x11)
        /// </summary>
        public int rangeMax;
        /// <summary>
        /// RGBA bytes, Red, Green, Blue, Alpha
        /// </summary>
        public byte[] buildColors;
        /// <summary>
        /// RGBA bytes, Red, Green, Blue, Alpha
        /// </summary>
        public byte[] destroyColors;
        /// <summary>
        /// RGBA bytes, Red, Green, Blue, Alpha
        /// </summary>
        public byte[] exchangeColors;
        /// <summary>
        /// A whitelist override for valid build-with blocks with a paste cost match.
        /// I hope this works with the config class :]
        /// </summary>        
        public Dictionary<string, int> whitelist;
        // default constructor/default values
        
        public BuildingGadgetsConfig()
        {
            useDurability = true;
            usePaste = true;
            rangeMax = 5;
            buildColors = new byte[] { 0, 0, 110, 110 };
            destroyColors = new byte[] { 110, 0, 0, 110 };
            exchangeColors = new byte[] { 110, 0, 110, 110 };

            whitelist = new Dictionary<string, int>
            {
                { "bigberrybush", 100 },
                { "smallberrybush", 100 }
            };
        }
    }
}
