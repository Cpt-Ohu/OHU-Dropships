using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class JobDriver_LoadCargoMultiple : JobDriver_HaulToContainer
    {
        private ShipBase ship
        {
            get
            {
               return (ShipBase)TargetB;
            }
        }

        private TransferableOneWay transferable
        {
            get
            {
                return TransferableUtility.TransferableMatchingDesperate(this.TargetA.Thing, ship.compShip.leftToLoad);
            }
        }


        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDespawnedOrNull(TargetIndex.B);
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1, 1, null);
            yield return Toils_Reserve.ReserveQueue(TargetIndex.A, 1, 1, null);
            //yield return Toils_Reserve.Reserve(TargetIndex.B, 10, 1, null);
            //yield return Toils_Reserve.ReserveQueue(TargetIndex.B, 10, 1, null);
            Toil toil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            //toil.AddFailCondition(() => ShipFull(ship));
            toil.tickAction += delegate
            {
                if (this.ShipFull(ship))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            yield return toil;
            yield return Toils_Construct.UninstallIfMinifiable(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            Toil toilPickup = Toils_Haul.StartCarryThing(TargetIndex.A, false, true);//.FailOn(() => this.ShipFull(ship));
            yield return toilPickup;
            yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(toil, TargetIndex.A);
            Toil toil2 = Toils_Haul.CarryHauledThingToContainer();

            toil2.tickAction += delegate
            {
                if (this.ShipFull(ship, false))
                {
                    this.EndJobWith(JobCondition.Incompletable);
                }
            };
            yield return toil2;
            yield return Toils_Goto.MoveOffTargetBlueprint(TargetIndex.B);
            yield return Toils_Construct.MakeSolidThingFromBlueprintIfNecessary(TargetIndex.B);
            Toil toil3 = Toils_Haul.DepositHauledThingInContainer(TargetIndex.B, TargetIndex.C);
            //toil3.AddFailCondition(() => this.ShipFull(ship));
            yield return toil3;
            yield return Toils_Haul.JumpToCarryToNextContainerIfPossible(toil2, TargetIndex.C);
            yield break;
        }


        private Action RestoreRemainingThings(Thing t, int amount)
        {
            return delegate
            {
                pawn.jobs.TryTakeOrderedJob(HaulAIUtility.HaulToStorageJob(pawn, t));
            };
        }

        private bool ShipFull(ShipBase ship, bool firstCheck = true)
        {
            //Log.Message("Checking");
            CompShip compShip = ship.compShip;
                if (transferable != null)
                {
                    if (firstCheck && this.CurJob.count > transferable.CountToTransfer)
                    {
                        return true;
                    }
                    if (!firstCheck && this.TargetA.Thing.stackCount > transferable.CountToTransfer)
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            
            
        }

    }
}
