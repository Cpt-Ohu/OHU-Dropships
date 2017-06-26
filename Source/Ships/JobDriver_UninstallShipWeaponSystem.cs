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

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Reserve.Reserve(TargetIndex.A, 10);
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
                    ShipBase ship = (ShipBase)TargetA.Thing;
                    Action action = delegate
                    {
                        Building_ShipTurret turret = (Building_ShipTurret)TargetC.Thing;
                        if (turret != null && ship.installedTurrets.ContainsValue(turret))
                        {
                            Thing t = ThingMaker.MakeThing(turret.installedByWeaponSystem);
                            GenSpawn.Spawn(t, TargetB.Thing.Position, this.Map);
                            ship.installedTurrets.Remove(turret.Slot);
                        }
                    };

                    ship.compShip.Notify_PawnEntered(this.pawn);

                    action();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield break;
        }
    }
}
