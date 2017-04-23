using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace OHUShips
{
    public class ShipWeaponSlot : ILoadReferenceable, IExposable
    {
        public string SlotName;
        public WeaponSystemType slotType;
        public Vector3 posOffset;

        public IntVec3 turretPosOffset;

        public IntVec2 turretMinSize = new IntVec2(1, 1);

        public AltitudeLayer altitudeLayer = AltitudeLayer.ItemImportant;

        public string GetUniqueLoadID()
        {
            return "WeaponSlot_" + DropShipUtility.currentShipTracker.GetNextWeaponSlotID();
        }

        public virtual void ExposeData()
        {
            Scribe_Values.LookValue<string>(ref this.SlotName, "SlotName", "");
        }
    }
}
