using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class WeaponSystem : ThingWithComps
    {
        protected StunHandler stunner;
        
        public bool isInstalled = false;

        protected Vector3 drawPosOffset;

        public string assignedSlotName;

        public ShipWeaponSlot slotToInstall;

        public WeaponSystemType weaponSystemType;

        protected LocalTargetInfo forcedTarget = LocalTargetInfo.Invalid;

        public WeaponSystem()
        {
            this.stunner = new StunHandler(this);
        }

        public override void Tick()
        {
            if (!this.Spawned)
            {
                base.Tick();
                this.stunner.StunHandlerTick();
            }
            if (this.isInstalled && this.Spawned)
            {
                this.DeSpawn();
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.isInstalled = false;

        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<StunHandler>(ref this.stunner, "stunner", new object[]
            {
                this
            });

            Scribe_Values.Look<bool>(ref this.isInstalled, "isInstalled", false, false);
            Scribe_Values.Look<string>(ref this.assignedSlotName, "assignedSlotName");
            Scribe_Values.Look<Vector3>(ref this.drawPosOffset, "drawPosOffset");
            Scribe_Values.Look<WeaponSystemType>(ref this.weaponSystemType, "weaponSystemType");
            Scribe_TargetInfo.Look(ref this.forcedTarget, "forcedTarget");
            if (this.slotToInstall != null)
            {
                Scribe_References.Look<ShipWeaponSlot>(ref this.slotToInstall, "slotToInstall");
            }
        }


        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
            if (absorbed)
            {
                return;
            }
            this.stunner.Notify_DamageApplied(dinfo, true);
            absorbed = false;
        }
        
        public bool ThreatDisabled()
        {
            CompPowerTrader comp = base.GetComp<CompPowerTrader>();
            if (comp == null || !comp.PowerOn)
            {
                    CompRefuelable comp3 = base.GetComp<CompRefuelable>();
                if (comp3 != null || !comp3.HasFuel)
                {
                    CompMannable comp2 = base.GetComp<CompMannable>();
                    if (comp2 == null || !comp2.MannedNow)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
