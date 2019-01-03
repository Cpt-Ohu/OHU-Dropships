using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class JobDriver_InstallShipWeaponSystem : JobDriver
    {
		//public override bool TryMakePreToilReservations(bool errorOnFail)
		//{
		//    throw new NotImplementedException();
		//}
		public override bool TryMakePreToilReservations(bool errorOnFail)
		{
			return true;
			//throw new NotImplementedException();
		}

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
            yield return Toils_Reserve.ReserveQueue(TargetIndex.A, 1);
            yield return Toils_Reserve.Reserve(TargetIndex.B, 10, 1);
            yield return Toils_Reserve.ReserveQueue(TargetIndex.B, 1);
            Toil toil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return toil;
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true);
            yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(toil, TargetIndex.A);
            Toil toil2 = Toils_Goto.Goto(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return toil2;
            Toil toil3 = new Toil();
            toil3.defaultCompleteMode = ToilCompleteMode.Delay;
            toil3.defaultDuration = 500;
            toil3.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            yield return toil3;
            yield return new Toil
            {
                initAction = delegate
                {
                    ShipBase ship = (ShipBase)TargetB.Thing;
                    ThingWithComps thing = (ThingWithComps)TargetA.Thing;
                    CompShipWeapon comp = thing.TryGetComp<CompShipWeapon>();
                    
                    Action action = delegate
                    {
                        if (comp != null)
                        {
                            switch (comp.SProps.weaponSystemType)
                            {
                                case WeaponSystemType.LightCaliber:
                                    {

                                        if (ship.TryInstallTurret(comp))
                                        {
                                            this.pawn.carryTracker.GetDirectlyHeldThings().Remove(TargetA.Thing);
                                            ship.weaponsToInstall.Remove(comp.slotToInstall);
                                        }
                                        break; 
                                    }
                                case WeaponSystemType.HeavyCaliber:
                                    {
                                        break;
                                    }
                                case WeaponSystemType.Bombing:
                                    {
                                        WeaponSystemShipBomb bomb = thing as WeaponSystemShipBomb;
                                        if (ship.TryInstallPayload(bomb, comp))
                                        {
                                            this.pawn.carryTracker.GetDirectlyHeldThings().Remove(TargetA.Thing);
                                            ship.weaponsToInstall.Remove(comp.slotToInstall);
                                        }
                                        break;
                                    }
                            }
                        }
                    };

                    action();
                },
                
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield break;
        }
    }
}
