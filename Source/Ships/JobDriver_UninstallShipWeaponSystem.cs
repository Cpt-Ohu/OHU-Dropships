using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse.AI;
using Verse;
using RimWorld;
using System.Diagnostics;

namespace OHUShips
{
    public class JobDriver_UninstallShipWeaponSystem : JobDriver
    {

        public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
			//throw new NotImplementedException();
		}

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
            //yield return Toils_Reserve.Reserve(TargetIndex.B, 1);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            Toil toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 500;
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            yield return toil;
            yield return new Toil
            {
                initAction = delegate
                {
                    ShipBase ship = (ShipBase)TargetB.Thing;
                    
                        Building_ShipTurret turret = (Building_ShipTurret)TargetA.Thing;
                    if (turret != null && ship.installedTurrets.ContainsValue(turret))
                    {
                        Thing t = ThingMaker.MakeThing(turret.installedByWeaponSystem);
                        GenSpawn.Spawn(t, TargetA.Thing.Position, this.Map);
                        ship.weaponsToUninstall.RemoveAll(x => x.Value == turret);
                        ship.installedTurrets[turret.Slot] = null;
                        ship.assignedTurrets.Remove(turret);
                        turret.Destroy();
                    }
                        
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield break;
        }
    }
}
