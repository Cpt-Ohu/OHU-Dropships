using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class WorkGiver_UninstallShipWeapon : WorkGiver_Scanner
    {

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
            }
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            ShipBase ship = (ShipBase)t;
            KeyValuePair<ShipWeaponSlot, Thing> weaponSpecs = ship.weaponsToUninstall.RandomElement();

            weaponSpecs.Value.TryGetComp<CompShipWeapon>().slotToInstall = weaponSpecs.Key;

            return new Job(ShipNamespaceDefOfs.UninstallShipWeapon, weaponSpecs.Value, ship)
            {
                count = 1,
                ignoreForbidden = false
            };

        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is ShipBase)
            {
                ShipBase ship = (ShipBase)t;
                return ship.weaponsToUninstall.Count > 0;
            }
            return false;
        }
    }
}

