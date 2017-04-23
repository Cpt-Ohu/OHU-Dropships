using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OHUShips
{
    public class CompProperties_Ship : CompProperties
    {
        public CompProperties_Ship()
        {
            this.compClass = typeof(CompShip);
        }

        public int TicksToImpact = 600;
        public int TicksToDespawn = 600;
        public int IncomingAngle = 35;
        public ThingDef FuelType = ShipNamespaceDefOfs.Chemfuel;
        public string ShadowGraphicPath;
        public int maxCargo = 1000;
        public int maxFuel = 1000;
        public int maxPassengers = 6;
        public List<FactionDef> availableToFactions = new List<FactionDef>();
        public ThingDef LeavingShipDef;
        public string FleetIconGraphicPath = "UI/Buttons/ButtonShip";
        public List<ShipWeaponSlot> weaponSlots = new List<ShipWeaponSlot>();
        public bool CanBeStartingShip = false;

    }
}
