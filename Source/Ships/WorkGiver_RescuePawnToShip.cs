using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class WorkGiver_RescuePawnToShip : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.OnCell;
            }
        }

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                return ThingRequest.ForGroup(ThingRequestGroup.Pawn);
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn pawn2 = t as Pawn;
            if (pawn2 == null || !pawn2.Downed || pawn2.Faction != pawn.Faction || pawn2.InBed() || !pawn.CanReserve(pawn2, 1, -1, null, forced) || GenAI.EnemyIsNear(pawn2, 5f))
            {
                return false;
            }
            Thing thing = this.FindShip(pawn);
            return thing != null && pawn2.CanReserve(thing, 1, -1, null, false);
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn pawn2 = t as Pawn;
            Thing t2 = this.FindShip(pawn);
            return new Job(ShipNamespaceDefOfs.RescueToShip, pawn2, t2)
            {
                count = 1                 
            };
        }

        public Thing FindShip(Pawn pawn)
        {

            List<ShipBase> allShips = DropShipUtility.ShipsOnMap(pawn.Map).FindAll(x => DropShipUtility.HasPassengerSeats(x) && x.Faction == pawn.Faction);
            if (allShips.NullOrEmpty())
            {
                return null;
            }
            return allShips.RandomElement();
        }
    }
}
