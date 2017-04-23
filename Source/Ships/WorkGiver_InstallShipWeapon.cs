using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class WorkGiver_InstallShipWeapon : WorkGiver_Scanner
    {


        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
            }
        }

        public override Job JobOnThing(Pawn pawn, Thing t)
        {
            ShipBase ship = (ShipBase)t;
            KeyValuePair<ShipWeaponSlot, Thing> weaponSpecs = ship.weaponsToInstall.RandomElement();
            if (!ship.Map.reservationManager.IsReserved(weaponSpecs.Value, pawn.Faction))
            {
                weaponSpecs.Value.TryGetComp<CompShipWeapon>().slotToInstall = weaponSpecs.Key;

                return new Job(ShipNamespaceDefOfs.InstallShipWeapon, weaponSpecs.Value, ship)
                {
                    count = 1,
                    ignoreForbidden = false
                };
            }
            return null;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t)
        {
            if (t is ShipBase)
            {
                ShipBase ship = (ShipBase)t;
                return ship.weaponsToInstall.Count > 0 && !t.Map.reservationManager.IsReserved(t, pawn.Faction);
            }
            return false;
        }
    }
}
