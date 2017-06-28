using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OHUShips
{
    public class LordToil_LoadShipCargo : LordToil
    {
        private ShipBase ship;

        public LordToil_LoadShipCargo(ShipBase ship)
        {
            this.ship = ship;
        }

        public override bool AllowSatisfyLongNeeds
        {
            get
            {
                return false;
            }
        }        

        public override void UpdateAllDuties()
        {
            for (int i = 0; i < this.lord.ownedPawns.Count; i++)
            {
                PawnDuty pawnDuty = new PawnDuty(ShipNamespaceDefOfs.LoadShipCargoDuty, ship);
                this.lord.ownedPawns[i].mindState.duty = pawnDuty;
            }
        }
    }
}
