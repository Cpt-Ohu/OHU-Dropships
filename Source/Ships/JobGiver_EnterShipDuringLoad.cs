using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class JobGiver_EnterShipDuringLoad : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            ShipBase ship = this.FindAppropriateShipToEnter(pawn);
            if (ship == null || !pawn.CanReserveAndReach(ship, PathEndMode.Touch, Danger.Deadly, 1))
            {
                return null;
            }
            return new Job(ShipNamespaceDefOfs.EnterShip, ship);
        }

        private ShipBase FindAppropriateShipToEnter(Pawn p)
        {
            if (p.mindState.duty != null && p.mindState.duty.focus != null)
            {
                ShipBase ship = (ShipBase)p.mindState.duty.focus;
                if (ship != null)
                {
                    List<TransferableOneWay> leftToLoad = ship.compShip.leftToLoad;
                    if (leftToLoad != null)
                    {
                        for (int j = 0; j < leftToLoad.Count; j++)
                        {
                            if (leftToLoad[j].AnyThing is Pawn)
                            {
                                List<Thing> things = leftToLoad[j].things;
                                for (int k = 0; k < things.Count; k++)
                                {
                                    if (things[k] == p)
                                    {
                                        return ship;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

    }
}
