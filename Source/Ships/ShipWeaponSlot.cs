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

        private int LoadID
        {
            get
            {
                if (this.loadID == -1)
                {
                    this.loadID = DropShipUtility.currentShipTracker.GetNextWeaponSlotID();
                }
                return this.loadID;
            }                
        }

        public IntVec3 turretPosOffset;

        public IntVec2 turretMinSize = new IntVec2(1, 1);

        public AltitudeLayer altitudeLayer = AltitudeLayer.ItemImportant;
               
        public string GetUniqueLoadID()
        {
            return "ShipWeaponSlot_" + this.LoadID;
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look<string>(ref this.SlotName, "SlotName", "");
            Scribe_Values.Look<int>(ref this.loadID, "loadID");
            Scribe_Values.Look<WeaponSystemType>(ref this.slotType, "slotType");
        }
    }
}
