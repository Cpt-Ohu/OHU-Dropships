using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OHUShips
{
    public class CompShipWeapon : ThingComp
    {
        public ShipWeaponSlot slotToInstall;

        public CompProperties_ShipWeapon SProps
        {
            get
            {
                return (CompProperties_ShipWeapon)this.props;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.LookReference<ShipWeaponSlot>(ref this.slotToInstall, "slotToInstall");
        }
    }
}
