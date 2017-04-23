using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OHUShips
{
    public class CompProperties_ShipWeapon : CompProperties
    {

        public CompProperties_ShipWeapon()
        {
            this.compClass = typeof(CompShipWeapon);
        }
        
        public List<ThingDef> availableToShips = new List<ThingDef>();

        public WeaponSystemType weaponSystemType;

        public bool canSwivel = true;

        public ThingDef TurretToInstall;

        public ThingDef PayloadToInstall;

    }
}
