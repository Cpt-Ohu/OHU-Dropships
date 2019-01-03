using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Verse;

namespace OHUShips
{
    public class WorldShipTrader : IExposable
    {
        public WorldShipTrader(WorldShip worldShip)
        {
            this._worldShip = worldShip;
        }

        private WorldShip _worldShip;

        public void ExposeData()
        {
            Scribe_References.Look<WorldShip>(ref this._worldShip, "WorldShip");
        }

        public IEnumerable<Thing> Goods
        {
            get
            {
                foreach (var data in _worldShip.WorldShipData)
                {
                    foreach (Thing t in data.Cargo)
                    {
                        yield return t;
                    }
                    foreach (Pawn p in data.LiveStock)
                    {
                        yield return p;
                    }
                    foreach (Pawn p in data.Prisoners)
                    {
                        yield return p;
                    }
                }
            }
        }

        public TraderKindDef TraderKind => null;


        [DebuggerHidden]
        public IEnumerable<Thing> ColonyThingsWillingToBuy()
        {
            foreach (Thing t in this.Goods)
            {
                yield return t;
            }
        }

        public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            var shipData = this._worldShip.WorldShipData.FirstOrDefault(d => d.Cargo.Contains(toGive));
            if (shipData != null && shipData.Ship != null)
            {
                shipData.Ship.GetDirectlyHeldThings().Remove(toGive);
            }
        }

        public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            ShipBase shipBase = this.GetShipWithCargoSpace();
            if (shipBase != null)
            {
                shipBase.GetDirectlyHeldThings().TryAddOrTransfer(toGive, countToGive, true);
                if (toGive is Pawn p)
                {
                    if (p.IsWorldPawn())
                    {
                        Find.WorldPawns.RemovePawn(p);
                    }
                }
            }
        }

        public ShipBase GetShipWithCargoSpace()
        {
            return this._worldShip.WorldShipData.Where(s => s.Ship.MassUsage < 1f).RandomElement()?.Ship;
        }
    }
}
