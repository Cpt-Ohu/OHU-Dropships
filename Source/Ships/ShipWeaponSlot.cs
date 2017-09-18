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
        private int loadID = -1;
                
        public IntVec3 turretPosOffset;

        public void InitiateWeaponSlotID()
        {
            if (this.loadID == -1)
            {
                this.loadID = DropShipUtility.currentShipTracker.GetNextWeaponSlotID();
            }
        }

        public ShipWeaponSlot()
        {

        }

        public ShipWeaponSlot(ShipWeaponSlot existingSlot)
        {
            this.SlotName = existingSlot.SlotName;
            this.slotType = existingSlot.slotType;
            this.posOffset = existingSlot.posOffset;
            this.turretPosOffset = existingSlot.turretPosOffset;
            this.InitiateWeaponSlotID();
        }

        public IntVec2 turretMinSize = new IntVec2(1, 1);

        public AltitudeLayer altitudeLayer = AltitudeLayer.ItemImportant;
               
        public string GetUniqueLoadID()
        {
            return "ShipWeaponSlot_" + this.loadID;
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look<string>(ref this.SlotName, "SlotName", "");
            Scribe_Values.Look<WeaponSystemType>(ref this.slotType, "slotType", WeaponSystemType.LightCaliber);
            Scribe_Values.Look<IntVec2>(ref this.turretMinSize, "turretMinSize", IntVec2.One);
            Scribe_Values.Look<IntVec3>(ref this.turretPosOffset, "turretPosOffset", IntVec3.Zero);
            Scribe_Values.Look<Vector3>(ref this.posOffset, "posOffset", Vector3.one);
            Scribe_Values.Look<int>(ref this.loadID, "loadID");
            Scribe_Values.Look<WeaponSystemType>(ref this.slotType, "slotType");
        }
    }
}
