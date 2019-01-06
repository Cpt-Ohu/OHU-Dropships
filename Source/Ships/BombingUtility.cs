using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace OHUShips
{
    public static class BombingUtility
    {
        public static bool TryBombWorldTarget(int targetTile, WorldShip worldShip)
        {
            MapParent parent = Find.WorldObjects.WorldObjectAt<MapParent>(targetTile);

            if (parent != null)
            {
                if (parent.HasMap)
                {
                    return BombingUtility.BombingRunOnMapCenter(parent.Map);
                }
                else
                {
                    Find.World.worldObjects.Remove(parent);
                }

                foreach (var data in worldShip.WorldShipData)
                {
                    ShipBase ship = data.Ship;
                    ship.loadedBombs.ForEach(c => c.Discard(true));
                }
            }
            return false;
        }

        private static bool BombingRunOnMapCenter(Map map)
        {
            throw new NotImplementedException();
        }

        private static void NotifySettlementBombed(Faction targetFaction, WorldShip worldShip)
        {
            Messages.Message("MessageBombedSettlement".Translate(), worldShip, MessageTypeDefOf.ThreatSmall);
            foreach (var faction in Find.FactionManager.AllFactionsVisible)
            {
                if (faction.RelationKindWith(targetFaction) != FactionRelationKind.Hostile)
                {
                    faction.RelationWith(worldShip.Faction).goodwill -= 5;
                }
            }
        }
    }
}
