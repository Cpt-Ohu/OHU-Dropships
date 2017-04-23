using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace OHUShips
{
    public class JobDriver_EnterShip : JobDriver
    {
        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Reserve.Reserve(TargetIndex.A, 10);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            Toil toil = new Toil();
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 50;
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            yield return toil;
            yield return new Toil
            {
                initAction = delegate
                {
                    ShipBase ship = (ShipBase)TargetA.Thing;
                    Action action = delegate
                    {
                        ship.TryAcceptThing(pawn, true);
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

