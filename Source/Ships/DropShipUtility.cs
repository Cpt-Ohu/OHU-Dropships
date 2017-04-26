using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace OHUShips
{
    [StaticConstructorOnStartup]
    public static class DropShipUtility
    {
        public static readonly Texture2D LaunchSingleCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true);
        public static readonly Texture2D LaunchFleetCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CommandLaunchFleet", true);
        public static readonly Texture2D LoadCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter", true);
        public static readonly Texture2D Info = ContentFinder<Texture2D>.Get("UI/Buttons/InfoButton", true);
        public static readonly Texture2D Rename = ContentFinder<Texture2D>.Get("UI/Buttons/Rename", true);
        public static readonly Texture2D shipButton = ContentFinder<Texture2D>.Get("UI/Buttons/ButtonShip", true);
        public static readonly Texture2D DropTexture = ContentFinder<Texture2D>.Get("UI/Buttons/UnloadShip", true);

        public static readonly Texture2D TouchDownCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CommandTouchDown", true);

        public static readonly Texture2D TradeCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/Trade", true);


        public static readonly Texture2D CancelTex = ContentFinder<Texture2D>.Get("UI/Buttons/ButtonDelete", true);
        public static readonly Texture2D movingFleet = ContentFinder<Texture2D>.Get("UI/Images/MovingFleet", true);
        public static readonly Texture2D movingShip = ContentFinder<Texture2D>.Get("UI/Images/MovingShip", true);

        public static readonly Texture2D TargeterShipAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/LaunchableMouseAttachment", true);
        
        public static ShipTracker currentShipTracker
        {
            get
            {
                ShipTracker tracker = Find.WorldObjects.AllWorldObjects.FirstOrDefault(x => x.def == ShipNamespaceDefOfs.ShipTracker) as ShipTracker;
                if (tracker == null)
                {
                    HarmonyPatches.GenerateFactionsIntoWorldPostFix();
                    return Find.WorldObjects.AllWorldObjects.FirstOrDefault(x => x.def == ShipNamespaceDefOfs.ShipTracker) as ShipTracker;
                }
                return tracker;
            }
        }

        public static void PassWorldPawnsForLandedShip(ShipBase ship)
        {
            List<Pawn> pawnsToMove = new List<Pawn>();
            foreach (Thing current in ship.GetInnerContainer())
            {
                Pawn pawn = current as Pawn;
                if (pawn != null && !ship.worldPawns.Contains(pawn))
                {
                    pawnsToMove.Add(pawn);
                }
            }

            ship.worldPawns.AddRange(pawnsToMove);
            ship.GetInnerContainer().RemoveAll(x => pawnsToMove.Contains<Pawn>(x as Pawn));
        }

        public static void ReimbarkWorldPawnsForLandedShip(ShipBase ship)
        {
            List<Thing> pawnsToMove = new List<Thing>();
            foreach (Pawn current in ship.worldPawns)
            {
                if (!ship.GetInnerContainer().Contains(current))
                {
                    pawnsToMove.Add(current);
                }
            }
            ship.GetInnerContainer().TryAddMany(pawnsToMove);
            ship.worldPawns.RemoveAll(x => pawnsToMove.Contains(x));
        }

        public static Vector3 DrawPosAt(ShipBase ship, int ticks, ShipBase_Traveling travelingShip = null)
        {
            if (ticks < 0)
            {
                ticks = 0;
            }

            Vector3 result = Gen.TrueCenter(ship);

            if (travelingShip != null)
            {
                result = Gen.TrueCenter(travelingShip.Position, travelingShip.containingShip.Rotation, travelingShip.containingShip.def.size, Altitudes.AltitudeFor(AltitudeLayer.FlyingItem));
            }
            result += DropShipUtility.drawOffsetFor(ship, ticks, false);

            return result;
        }

        public static Vector3 drawOffsetFor(ShipBase ship, int ticks, bool isShadow = false)
        {
            int angle = ship.compShip.sProps.IncomingAngle;            
            float num = (float)(ticks * ticks) * 0.01f;
            int sign = 1;
            int signHorizontal = 1;
            if (ship.shipState == ShipState.Outgoing)
            {
                sign = -1;
            }
            if (isShadow)
            {
                signHorizontal = 0;
            }

            int switchInt = ship.Rotation.AsInt;
            switch (switchInt)
            {
                case 0:
                    return new Vector3(sign * num * Mathf.Cos(angle), 0f, signHorizontal * num * Mathf.Sin(angle));
                case 1:
                    return new Vector3(0f, 0f, sign * num * Mathf.Sin(angle));
                case 2:
                    return new Vector3(sign * -num * Mathf.Cos(angle), 0f, signHorizontal * num * Mathf.Sin(angle));
                case 3:
                    return new Vector3(0f, 0f, sign * -num * Mathf.Sin(angle));
                default:
                    Log.Error("Ship with no Rot4 found");
                    return Vector3.zero;
            }
        }

        public static IntVec3 AdjustedIntVecForShip(ShipBase ship, IntVec3 inputIntVec)
        {
            switch (ship.Rotation.AsInt)
            {
                case 0:
                    return new IntVec3(inputIntVec.x, 0, inputIntVec.z);
                case 1:
                    return new IntVec3(-inputIntVec.z, 0, -inputIntVec.x);
                case 2:
                    return new IntVec3(-inputIntVec.x, 0, inputIntVec.z);
                case 3:
                    return new IntVec3(inputIntVec.z, 0, inputIntVec.x);
                default:
                    Log.Error("Ship with no Rot4 found");
                    return IntVec3.North;

            }
        }

        private static MaterialPropertyBlock shadowPropertyBlock = new MaterialPropertyBlock();

        public static void DrawDropSpotShadow(ShipBase ship, int ticks, ShipBase_Traveling travelingShip = null)
        {
            if (ticks < 0)
            {
                ticks = 0;
            }
                        
            Vector3 result = Gen.TrueCenter(ship);
            if (travelingShip != null)
            {
                result = travelingShip.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.FlyingItem);
            }
            result += DropShipUtility.drawOffsetFor(ship, ticks, true);
            result.y = Altitudes.AltitudeFor(AltitudeLayer.Shadows);

            Color white = Color.white;

            white.a = Mathf.InverseLerp(200f, 150f, (float)ticks);

            DropShipUtility.shadowPropertyBlock.SetColor(ShaderIDs.ColorId, white);
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(result, ship.compShip.parent.Rotation.AsQuat, new Vector3(1f, 1f, 1f));
            Graphics.DrawMesh(ship.compShip.parent.Graphic.MeshAt(ship.compShip.parent.Rotation), matrix, ship.compShip.dropShadow.MatSingle, 0, null, 0, DropShipUtility.shadowPropertyBlock);
        }

        public static void InitializeDropShipSpawn(ShipBase ship)
        {
        //    ship.def.selectable = false;
        }

        public static void DropShipLanded(ShipBase ship)
        {
         //   ship.def.selectable = true;
        }

        public static List<Thing> CurrentFactionShips(Pawn pawn)
        {
            List<Thing> list = pawn.Map.listerThings.AllThings.FindAll(x => x is ShipBase && x.Faction == pawn.Faction);

            return list;
        }

        public static List<ThingDef> AvailableDropShipsForFaction(Faction faction)
        {
            return DefDatabase<ThingDef>.AllDefsListForReading.FindAll(x => x.thingClass == typeof(ShipBase) && x.GetCompProperties<CompProperties_Ship>().availableToFactions.Contains(faction.def));
        }

        public static bool FactionHasDropShips(Faction faction)
        {
            if (!AvailableDropShipsForFaction(faction).NullOrEmpty())
            {
                return true;
            }
            return false;
        }

        public static List<ShipBase> CreateDropShips(List<Pawn> pawns, Faction faction, List<ThingDef> fixedShipDefs = null)
        {
            List<ShipBase> shipsToDrop = new List<ShipBase>();
            List<ThingDef> defs = new List<ThingDef>();
            if (fixedShipDefs.NullOrEmpty())
            {
                defs.AddRange(DropShipUtility.AvailableDropShipsForFaction(faction));
            }
            else
            {
                defs.AddRange(fixedShipDefs);
            }
            defs.OrderBy(x => x.GetCompProperties<CompProperties_Ship>().maxPassengers);
            int num = 0;
            while (num < pawns.Count)
            {
                ShipBase newShip = (ShipBase)ThingMaker.MakeThing(defs.RandomElementByWeight(x => x.GetCompProperties<CompProperties_Ship>().maxPassengers));
                newShip.SetFaction(faction);
                newShip.ShouldSpawnFueled = true;
                shipsToDrop.Add(newShip);
                num += newShip.compShip.sProps.maxPassengers;
            }
            DropShipUtility.LoadNewCargoIntoRandomShips(pawns.Cast<Thing>().ToList(), shipsToDrop);
            return shipsToDrop;
        }
                
        public static List<Pawn> AllPawnsInShip(ShipBase ship)
        {
            List<Pawn> tmp = new List<Pawn>();
            for (int i = 0; i > ship.GetInnerContainer().Count; i++)
            {
                Pawn pawn = ship.GetInnerContainer()[i] as Pawn;
                if (pawn != null)
                {
                    tmp.Add(pawn);
                }                    
            }

            return tmp;
        }
        
        public static void DropShipGroups(IntVec3 dropCenter, Map map, List<ShipBase> shipsToDrop, TravelingShipArrivalAction arrivalAction)
        {
            foreach (ShipBase current in shipsToDrop)
            {
                IntVec3 dropLoc;
                //      if (TryFindShipDropLocationNear(dropCenter, 200, map, out dropLoc, current.def.size))
                //   if (DropCellFinder.TryFindRaidDropCenterClose(out dropLoc, map))
                try
                {
                    if (!DropShipUtility.TryFindShipDropSpotNear(current, dropCenter, map, out dropLoc, true, true))
                    {
                        DropShipUtility.TryFindShipDropSpotNear(current, DropCellFinder.FindRaidDropCenterDistant(map), map, out dropLoc, true, true);
                    }
                        current.drawTickOffset = current.compShip.sProps.TicksToImpact + Rand.Range(10, 60);
                        current.ActivatedLaunchSequence = false;
                        current.shipState = ShipState.Incoming;
                        ShipBase_Traveling incomingShip = new ShipBase_Traveling(current, false, arrivalAction);
                        //             Log.Message("Dropping " + incomingShip.containingShip.ShipNick);
                        GenSpawn.Spawn(incomingShip, dropLoc, map);
                    }
                
                catch
                {
                }
            }
        }

        public static bool TryFindShipDropSpotNear(ShipBase ship, IntVec3 center, Map map, out IntVec3 result, bool allowFogged, bool canRoofPunch)
        {
            if (DebugViewSettings.drawDestSearch)
            {
                map.debugDrawer.FlashCell(center, 1f, "center");
            }

            Predicate<IntVec3> validatingExistingShips = (IntVec3 c) =>
            {
                Vector3 drawSize = ship.def.graphicData.drawSize;
                List<Thing> ships = map.listerThings.AllThings.FindAll(x => x is ShipBase_Traveling || x is ShipBase);
                for (int i = 0; i < ships.Count; i++)
                {
                    if (ships[i].Position.InHorDistOf(c, Math.Max(drawSize.x, drawSize.z)))
                    {
                        return false;
                    }
                }
                return true;
            };


            Predicate<IntVec3> validator = (IntVec3 c) => validatingExistingShips(c) &&  DropCellFinder.IsGoodDropSpot(c, map, allowFogged, canRoofPunch) && map.reachability.CanReach(center, c, PathEndMode.OnCell, TraverseMode.PassDoors, Danger.Deadly);
            int num = 5;
            while (!CellFinder.TryFindRandomCellNear(center, map, num, validator, out result))
            {
                num += 3;
                if (num > 29)
                {
                    result = center;
                    Log.Error("NoDropPoint found");
                    return false;
                }
            }
            return true;
        }


        public static bool TryFindShipDropLocationNear(IntVec3 center, int maxAcceptableDistance, Map map, out IntVec3 result, IntVec2 size)
        {
            int num = 0;
            while (num < 5)
            {
                Predicate<IntVec3> validator = delegate (IntVec3 c)
                {                    
                    foreach (IntVec3 current2 in GenAdj.CellsOccupiedBy(c, Rot4.North, size))
                    {
                        if (!current2.Standable(map))
                        {
                            return false;
                        }
                        if (map.roofGrid.Roofed(current2) && current2.GetRoof(map) != null)
                        {
                            return false;
                        }

                    }
                    if (map.IsPlayerHome)
                    {
                        return map.reachability.CanReachColony(c);
                    }
                    else
                    {
                        return true;
                    }
                };
                for (int i = 0; i < 1000; i++)
                {
                    CellFinder.TryFindRandomCellNear(center, map, maxAcceptableDistance, validator, out result);
                    if (validator(result))
                    {
                        return true;
                    }
                    else
                    {
                        result = CellFinder.RandomEdgeCell(map);
                        if (validator(result))
                        {
                            return true;
                        }
                    }
                }
                num++;
            }
            result = IntVec3.Invalid;
            return false;
        }

        public static List<WeaponSystem> availableWeaponSystemsForSlot(Map map, ShipWeaponSlot slot)
        {
            List<Thing> list = map.listerThings.AllThings.FindAll(x => x.TryGetComp<CompShipWeapon>() != null);

            List<WeaponSystem> list2 = new List<WeaponSystem>();
            for (int i=0; i < list.Count; i++)
            {
                CompShipWeapon comp = list[i].TryGetComp<CompShipWeapon>();
                if (comp.SProps.weaponSystemType == slot.slotType)
                {
                    list2.Add((WeaponSystem)list[i]);
                }
            }
            return list2;
        }

        public static List<Thing> availableWeaponsForSlot(Map map, ShipWeaponSlot slot)
        {
            return map.listerThings.AllThings.FindAll(x => x.TryGetComp<CompShipWeapon>() != null && x.TryGetComp<CompShipWeapon>().SProps.weaponSystemType == slot.slotType);            
        }

        public static float ApproxDaysWorthOfFood_Ship(ShipBase ship, List<TransferableOneWay> transferables)
        {
            List<TransferableOneWay> tmp = new List<TransferableOneWay>();
            tmp.AddRange(transferables);

            List<TransferableOneWay> tmpPawns = new List<TransferableOneWay>();
            List<TransferableOneWay> tmpItems = new List<TransferableOneWay>();
            foreach (Pawn current in ship.GetInnerContainer().Where(x => x is Pawn))
            {
                if (!current.RaceProps.Eats(FoodTypeFlags.Plant))
                {
                    DropShipUtility.AddThingsToTransferables(tmp, current);
                }
            }
            for (int i = 0; i < ship.GetInnerContainer().Count; i++)
            {
                if (!(ship.GetInnerContainer()[i] is Pawn))
                {
                    DropShipUtility.AddThingsToTransferables(tmp, ship.GetInnerContainer()[i]);
                }
            }

            return DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(tmp);
        }

        private static void AddThingsToTransferables(List<TransferableOneWay> transferables, Thing thing)
        {
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(thing, transferables);
            if (transferableOneWay == null)
            {
                transferableOneWay = new TransferableOneWay();
                transferables.Add(transferableOneWay);
            }
            transferableOneWay.things.Add(thing);
            transferableOneWay.countToTransfer = thing.stackCount;
        }


        public static bool LoadNewCargoIntoRandomShips(List<Thing> newCargo, List<ShipBase> ships, bool ignoreShipload = false)
        {
            for (int i = 0; i < newCargo.Count; i++)
            {
                int num = 0;
                while (!ships.RandomElement().TryAcceptThing(newCargo[i], true))
                {
                    ships.RandomElement().TryAcceptThing(newCargo[i], true);
                    num++;
                }

                if (num > ships.Count && !ignoreShipload)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
