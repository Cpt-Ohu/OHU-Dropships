using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OHUShips
{
    static class PawnExtensions
    {
        public static WorldShip GetWorldShip(this Pawn pawn)
        {
            return DropShipUtility.CurrentShipTracker.AllWorldShips.FirstOrDefault(x => x.WorldShipData.Any(d => d.Passengers.Contains(pawn)));
        }
    }
}
