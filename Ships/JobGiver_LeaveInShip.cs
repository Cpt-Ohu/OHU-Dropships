using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class JobGiver_LeaveInShip : ThinkNode_JobGiver
    {       
        protected override Job TryGiveJob(Pawn pawn)
        {
            List<Thing> ships = DropShipUtility.CurrentFactionShips(pawn).FindAll(x => x.Map == pawn.Map);
            if (!ships.NullOrEmpty())
            {
                Thing ship = ships.RandomElement();
                if (ship != null && ship.Map.reservationManager.CanReserve(pawn, ship, ship.TryGetComp<CompShip>().sProps.maxPassengers))
                {
                    Job job = new Job(ShipNamespaceDefOfs.LeaveInShip, pawn, ship);

                    return job;
                }
            }
            return null;
        }

    }
}
