using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace OHUShips
{
    [DefOf]
    public class ShipNamespaceDefOfs
    {
        public static WorldObjectDef ShipTracker;
        public static WorldObjectDef TravelingSuborbitalShip;
        public static WorldObjectDef LandedShip;
        public static WorldObjectDef ShipDropSite;

        public static JobDef UninstallShipWeapon;

        public static JobDef InstallShipWeapon;
        public static JobDef EnterShip;
        public static JobDef LoadContainerMultiplePawns;

        public static JobDef LeaveInShip;

        public static DutyDef LeaveInShipDuty;
        public static DutyDef StealForShipDuty;

        public static DutyDef LoadShipCargo;
        





        public static ThingDef Chemfuel;
    }
}
