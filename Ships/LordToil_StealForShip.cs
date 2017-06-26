using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse.AI;

namespace OHUShips
{
    public class LordToil_StealForShip : LordToil_StealCover
    {
        protected override DutyDef DutyDef
        {
            get
            {
                return ShipNamespaceDefOfs.StealForShipDuty;
            }
        }
    }
}
