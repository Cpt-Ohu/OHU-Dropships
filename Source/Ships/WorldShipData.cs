using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OHUShips
{
    public class WorldShipData : IExposable
    {

        public WorldShipData()
        {

        }

        public WorldShipData(ShipBase ship)
        {
            this.Ship = ship;
        }

        public ShipBase Ship;

        public List<Pawn> Passengers
        {
            get
            {
                return Ship.GetDirectlyHeldThings().Where(t => t is Pawn && t.def.race.Humanlike).Cast<Pawn>().ToList();
            }
        }
        public List<Thing> Prisoners
        {
            get
            {
                return Ship.GetDirectlyHeldThings().Where(t => t is Pawn && ((Pawn)t).IsPrisoner).ToList();
            }
        }

        public List<Thing> LiveStock
        {
            get
            {
                return Ship.GetDirectlyHeldThings().Where(t => (t is Pawn) && (t.def.race?.Humanlike == false)).ToList();
            }
        }
        public List<Thing> Cargo
        {
            get
            {
                return Ship.GetDirectlyHeldThings().Where(t => !(t is Pawn)).ToList();
            }

        }

        public void ExposeData()
        {
            Scribe_Deep.Look<ShipBase>(ref this.Ship, "Ship", false);
        }
    }
}
