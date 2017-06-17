using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OHUShips
{
    public class LoadShipCargoUtility
    {
        private static HashSet<Thing> neededThings = new HashSet<Thing>();

        public static Job JobLoadShipCargo(Pawn p, ShipBase ship)
        {
            Thing thing = LoadShipCargoUtility.FindThingToLoad(p, ship);
            return new Job(ShipNamespaceDefOfs.LoadContainerMultiplePawns, thing, ship)
            {
                count = Mathf.Min(TransferableUtility.TransferableMatching<TransferableOneWay>(thing, ship.compShip.leftToLoad).CountToTransfer, thing.stackCount),
                ignoreForbidden = true
            };
        }

        private static Thing FindThingToLoad(Pawn p, ShipBase ship)
        {
            LoadShipCargoUtility.neededThings.Clear();
            List<TransferableOneWay> leftToLoad = ship.compShip.leftToLoad;
            if (leftToLoad != null)
            {
                for (int i = 0; i < leftToLoad.Count; i++)
                {
                    TransferableOneWay transferableOneWay = leftToLoad[i];
                    if (transferableOneWay.CountToTransfer > 0)
                    {
                        for (int j = 0; j < transferableOneWay.things.Count; j++)
                        {
                            LoadShipCargoUtility.neededThings.Add(transferableOneWay.things[j]);
                        }
                    }
                }
            }
            if (!LoadShipCargoUtility.neededThings.Any<Thing>())
            {
                return null;
            }
            Predicate<Thing> validator = (Thing x) => LoadShipCargoUtility.neededThings.Contains(x) && p.CanReserve(x, 1);
            Thing thing = GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.Touch, TraverseParms.For(p, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator, null);
            if (thing == null)
            {
                foreach (Thing current in LoadShipCargoUtility.neededThings)
                {
                    Pawn pawn = current as Pawn;
                    if (pawn != null && (!pawn.IsColonist || pawn.Downed) && p.CanReserveAndReach(pawn, PathEndMode.Touch, Danger.Deadly, 1))
                    {
                        return pawn;
                    }
                }
            }
            LoadShipCargoUtility.neededThings.Clear();
            return thing;
        }

        public static bool HasJobOnShip(Pawn pawn, ShipBase ship)
        {
            return !ship.IsForbidden(pawn) && ship.compShip.AnythingLeftToLoad && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && pawn.CanReserveAndReach(ship, PathEndMode.Touch, pawn.NormalMaxDanger(), 10) && LoadShipCargoUtility.FindThingToLoad(pawn, ship) != null;
        }

        public static Lord FindLoadLord(ShipBase ship, Map map)
        {
            List<Lord> lords = map.lordManager.lords;
            for (int i = 0; i < lords.Count; i++)
            {
                LordJob_LoadShipCargo lordJob_LoadAndEnterTransporters = lords[i].LordJob as LordJob_LoadShipCargo;
                if (lordJob_LoadAndEnterTransporters != null && lordJob_LoadAndEnterTransporters.ship == ship)
                {
                    return lords[i];
                }
            }
            return null;
        }

        
    }
}
