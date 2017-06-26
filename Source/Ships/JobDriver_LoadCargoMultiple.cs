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
        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
            ShipBase ship = (ShipBase)TargetB;
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
            yield return Toils_Reserve.ReserveQueue(TargetIndex.A, 1);
            yield return Toils_Reserve.Reserve(TargetIndex.B, 10, 1);
            yield return Toils_Reserve.ReserveQueue(TargetIndex.B, 10, 1);
            Toil toil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return toil;
            yield return Toils_Construct.UninstallIfMinifiable(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, true);
            yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(toil, TargetIndex.A);
            Toil toil2 = Toils_Haul.CarryHauledThingToContainer();
            toil2.AddFinishAction(delegate
            {
                ship.compShip.SubtractFromToLoadList(TargetA.Thing);
            });
            yield return toil2;
            yield return Toils_Goto.MoveOffTargetBlueprint(TargetIndex.B);
            yield return Toils_Construct.MakeSolidThingFromBlueprintIfNecessary(TargetIndex.B);
            yield return Toils_Haul.DepositHauledThingInContainer(TargetIndex.B, TargetIndex.C);
            yield return Toils_Haul.JumpToCarryToNextContainerIfPossible(toil2, TargetIndex.C);
            yield break;
        }
    }
}
