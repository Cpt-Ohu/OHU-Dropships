using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class JobGiver_FleeIfShipDestroyed : JobGiver_ExitMapBest
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
           if (DropShipUtility.LordShipsDestroyed(pawn))
            {
                return base.TryGiveJob(pawn);
            }
            return null;    
        }
    }
}
