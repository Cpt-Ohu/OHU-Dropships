using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;
using System.Reflection;

namespace OHUShips
{
    public class Building_ShipTurret : Building_TurretGun
    {
        private ShipBase parentShipCached;

        public ShipBase ParentShip
        {
            get
            {
                if (this.parentShipCached == null)
                {
                    this.parentShipCached = DropShipUtility.currentShipTracker.AllWorldShips.FirstOrDefault(x => x.GetUniqueLoadID() == this.parentShipLoadID);
                }
                return this.parentShipCached;
            }
        }
        
        public string assignedSlotName;

        public ThingDef installedByWeaponSystem;

        private string parentShipLoadID = "";

        public void AssignParentShip(ShipBase ship)
        {
            this.parentShipLoadID = ship.GetUniqueLoadID();
        }
        
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public void SwitchTurret(bool active)
        {
            bool flag = (bool)typeof(Building_TurretGun).GetProperty("holdFire").GetValue(this, null);

            flag = active;
            if (flag)
            {
                LocalTargetInfo info = (LocalTargetInfo)typeof(Building_TurretGun).GetProperty("currentTargetInt").GetValue(this, null);
                int burstTicks = (int)typeof(Building_TurretGun).GetProperty("burstWarmupTicksLeft").GetValue(this, null);
                info = LocalTargetInfo.Invalid;
                burstTicks = 0;
            }
        }

        public ShipWeaponSlot Slot
        {
            get
            {
                if(this.parentShipCached != null)
                {
                    ShipWeaponSlot slot = parentShipCached.installedTurrets.First(x => x.Key.SlotName == this.assignedSlotName).Key;
                    if (slot != null)
                    {
                        return slot;
                    }
                    else
                    {
                        Log.Error("No slot found for " + this.ToString() + " on " + parentShipCached.ToString());
                        return null;
                    }
                }
                Log.Error("Requested ShipWeaponSlot on Turret without assigned Ship");
                return null;
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (this.ParentShip != null)
                {
                    KeyValuePair<ShipWeaponSlot, Building_ShipTurret> turretEntry = ParentShip.installedTurrets.FirstOrDefault(x => x.Value == this);
                    if (turretEntry.Key != null)
                    {
                        Vector3 vector = this.ParentShip.DrawPos + DropShipUtility.AdjustedIntVecForShip(this.ParentShip, turretEntry.Key.turretPosOffset).ToVector3();
                        vector.y = Altitudes.AltitudeFor(turretEntry.Key.altitudeLayer);
                        return vector;
                    }
                }
                
                return base.DrawPos;
            }
        }

        public override void Draw()
        {
            this.top.DrawTurret();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerator<Gizmo> enumerator = base.GetGizmos().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Gizmo current = enumerator.Current;
                yield return current;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref this.assignedSlotName, "assignedSlotName");
            Scribe_Values.Look<string>(ref this.parentShipLoadID, "parentShipLoadID");
            Scribe_Defs.Look<ThingDef>(ref this.installedByWeaponSystem, "installedByWeaponSystem");
        }
    }
}
