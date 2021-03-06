﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse.AI;
using Verse;

namespace OHUShips
{
    public class JobDriver_LeaveInShip : JobDriver
    {
        protected ShipBase ship
        {
            get
            {
                return (ShipBase)base.job.GetTarget(TargetIndex.B).Thing;
            }
        }
                       
        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (this.pawn.carryTracker.CarriedThing != null)
            {
                yield return Toils_Haul.CarryHauledThingToContainer();
            }
            yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.ClosestTouch);

            Toil leaving = JobDriver_LeaveInShip.EnterShip(this.GetActor(), ship);
            leaving.AddFinishAction(delegate
            {
                if (ship.pilotPresent)
                {
                    ship.PrepareForLaunchIn(1000);
                }

            });
            yield return leaving;
            yield break;
        }

        public static Toil EnterShip(Pawn pawn, ShipBase ship)
        {
            Toil gotoShip = new Toil
            {
                initAction = delegate
                {
                    if (pawn.carryTracker.CarriedThing != null)
                    {
                        ship.TryAcceptThing(pawn.carryTracker.CarriedThing);
                    }
                    ship.TryAcceptThing(pawn, true);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            return gotoShip;
        }

        public override bool TryMakePreToilReservations(bool failOnError)
        {
            return this.pawn.Reserve(this.job.GetTarget(TargetIndex.A), this.job, ship.compShip.sProps.maxPassengers, 1, null);
        }
    }
}
