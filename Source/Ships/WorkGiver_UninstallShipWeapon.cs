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
            Building_ShipTurret turret = t as Building_ShipTurret;

            if (turret != null && turret.ParentShip != null)
            {
                //KeyValuePair<ShipWeaponSlot, Thing> weaponSpecs = turret.ParentShip.weaponsToUninstall.FirstOrDefault(x => x.Value == turret);
                
                if (turret.ParentShip.weaponsToUninstall.ContainsValue(turret) && pawn.Map.reservationManager.CanReserve(pawn, turret,1))
                {
                        return new Job(ShipNamespaceDefOfs.UninstallShipWeapon, turret, turret.ParentShip)
                        {
                            count = 1,
                            ignoreForbidden = false,
                        };                    
                }
            }
            return null;       
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is Building_ShipTurret)
            {
                Building_ShipTurret turret = t as Building_ShipTurret;
                if (turret.ParentShip != null)
                {
                    return pawn.Map.reservationManager.CanReserve(pawn, turret, 1) && turret.ParentShip.weaponsToUninstall.Count > 0 && !turret.ParentShip.weaponsToUninstall.Any(x => x.Value == null);
                }
            }
            return false;
        }
    }
}

