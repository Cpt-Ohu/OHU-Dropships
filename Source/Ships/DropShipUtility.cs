using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public static readonly Texture2D ParkingSingle = ContentFinder<Texture2D>.Get("UI/Commands/CommandParkingShipSingle", true);
        public static readonly Texture2D ParkingFleet = ContentFinder<Texture2D>.Get("UI/Commands/CommandParkingShipFleet", true);
        public static readonly Texture2D ReturnParkingSingle = ContentFinder<Texture2D>.Get("UI/Commands/CommandLaunchParkingSingle", true);
        public static readonly Texture2D ReturnParkingFleet = ContentFinder<Texture2D>.Get("UI/Commands/CommandLaunchParkingFleet", true);

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


        public static void ReimbarkWorldPawnsForShip(ShipBase ship)
        {
            List<Thing> pawnsToMove = new List<Thing>();
            foreach (Pawn current in ship.GetDirectlyHeldThings())
            {
                current.holdingOwner = ship.GetDirectlyHeldThings();
                if (!ship.GetDirectlyHeldThings().Contains(current))
                {
                    pawnsToMove.Add(current);
                }
            }
            ship.GetDirectlyHeldThings().TryAddRangeOrTransfer(pawnsToMove);
        }

        public static List<ShipBase> ShipsOnMap(Map map)
        {
            return map.listerThings.AllThings.FindAll(x => x is ShipBase).Cast<ShipBase>().ToList();
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
                result = Gen.TrueCenter(travelingShip.Position, travelingShip.containingShip.Rotation, travelingShip.containingShip.def.size, Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays));
            }
            result += DropShipUtility.drawOffsetFor(ship, ticks, false);

            return result;
        }

        public static Vector3 drawOffsetFor(ShipBase ship, int ticks, bool isShadow = false)
        {
            float angle = ship.compShip.sProps.IncomingAngle * Mathf.PI / 180f;
            float num = (float)(ticks * ticks) * 0.01f;
            int sign = 1;
            int signHorizontal = 1;
            if (ship.shipState == ShipState.Incoming)
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
                    return new Vector3(0f, 0f, sign * -num * Mathf.Sin(angle));
                case 2:
                    return new Vector3(sign * -num * Mathf.Cos(angle), 0f, signHorizontal * num * Mathf.Sin(angle));
                case 3:
                    return new Vector3(0f, 0f, sign * num * Mathf.Sin(angle));
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
                result = travelingShip.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Skyfaller);
            }
            result += DropShipUtility.drawOffsetFor(ship, ticks, true);
            result.y = Altitudes.AltitudeFor(AltitudeLayer.Shadows);

            Color white = Color.white;

            white.a = Mathf.InverseLerp(200f, 150f, (float)ticks);

            DropShipUtility.shadowPropertyBlock.SetColor(Shader.PropertyToID("Cutout"), white);
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
            for (int i = 0; i > ship.GetDirectlyHeldThings().Count; i++)
            {
                Pawn pawn = ship.GetDirectlyHeldThings()[i] as Pawn;
                if (pawn != null)
                {
                    tmp.Add(pawn);
                }                    
            }

            return tmp;
        }

        public static bool ShipIsAlreadyDropping(ShipBase ship, Map map)
        {
            if(ship.Spawned)
            {
                return true;
            }
            IEnumerator<Thing> enumerator = map.listerThings.AllThings.Where(x => x is ShipBase_Traveling).GetEnumerator();
            while (enumerator.MoveNext())
            {
                ShipBase_Traveling current = enumerator.Current as ShipBase_Traveling;
                if (current.containingShip == ship)
                {
                    return true;
                }

            }
            return false;
        }
        public static void DropShipGroups(IntVec3 dropCenter, Map map, List<ShipBase> shipsToDrop, TravelingShipArrivalAction arrivalAction, bool launchdAsSingleShip = false)
        {
            foreach (ShipBase current in shipsToDrop.Where(x => !DropShipUtility.ShipIsAlreadyDropping(x, map)))
            {
                current.shouldSpawnTurrets = true;
                IntVec3 dropLoc;
                //      if (TryFindShipDropLocationNear(dropCenter, 200, map, out dropLoc, current.def.size))
                //   if (DropCellFinder.TryFindRaidDropCenterClose(out dropLoc, map))
                try
                {

                    dropLoc = dropCenter;
                    if (dropLoc.IsValid && launchdAsSingleShip)
                    {
                    }
                    else
                    {
                        if (!DropShipUtility.TryFindShipDropSpotNear(current, dropCenter, map, out dropLoc, true, true))
                        {
                            DropShipUtility.TryFindShipDropSpotNear(current, DropCellFinder.FindRaidDropCenterDistant(map), map, out dropLoc, true, true);
                        }
                    }
                    current.drawTickOffset = current.compShip.sProps.TicksToImpact + Rand.Range(10, 60);
                    current.ActivatedLaunchSequence = false;
                    current.shipState = ShipState.Incoming;
                    ShipBase_Traveling incomingShip = new ShipBase_Traveling(current, false, arrivalAction);
                    GenSpawn.Spawn(incomingShip, dropLoc, map);
                }

                catch (Exception ex)
                {
                    Log.Error("Couldn't drop ships in map: " + ex.ToString());
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

        private static List<TransferableOneWay> tmpTransferables = new List<TransferableOneWay>();

        public static float ApproxDaysWorthOfFood_Ship(ShipBase ship, List<TransferableOneWay> transferables, bool canEatPlants)
        {            
            tmpTransferables.Clear();

            for (int i=0; i < transferables.Count; i++)
            {
                TransferableOneWay oneWay = new TransferableOneWay();
                oneWay.things.AddRange(transferables[i].things);
                oneWay.AdjustTo(transferables[i].CountToTransfer);
                Pawn pawn = oneWay.AnyThing as Pawn;

                tmpTransferables.Add(oneWay);
            }

            foreach (Pawn current in ship.GetDirectlyHeldThings().Where(x => x is Pawn))
            {
                    DropShipUtility.AddThingsToTransferables(tmpTransferables, current);
            }
            for (int i = 0; i < ship.GetDirectlyHeldThings().Count; i++)
            {
                if (!(ship.GetDirectlyHeldThings()[i] is Pawn))
                {
                    DropShipUtility.AddThingsToTransferables(tmpTransferables, ship.GetDirectlyHeldThings()[i]);
                }
            }
            return DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(tmpTransferables, canEatPlants, IgnorePawnsInventoryMode.DontIgnore);            
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
            DropShipUtility.AdjustToOneWayReflection(transferableOneWay, thing.stackCount);
        }

        private static void AdjustToOneWayReflection(TransferableOneWay transferable, int amount)
        {
            int count = Traverse.Create(transferable).Field("countToTransfer").GetValue<int>();
            count = amount;
        }


        public static bool LoadNewCargoIntoRandomShips(List<Thing> newCargo, List<ShipBase> ships, bool ignoreShipload = false)
        {
            for (int i = 0; i < newCargo.Count; i++)
            {
                newCargo[i].holdingOwner = null;
                int num = 0;
                while (!ships.RandomElement().TryAcceptThing(newCargo[i], true))
                {
                    Pawn pawn = newCargo[i] as Pawn;
                    ships.RandomElement().TryAcceptThing(newCargo[i], true);
                    
                    num++;
                }

                if (num > ships.Count && !ignoreShipload)
                {
                    break;
                }
            }
            return true;
        }

        public static bool LordShipsDestroyed(Pawn pawn)
        {
            LordJob_AerialAssault lordAssault = pawn.GetLord().LordJob as LordJob_AerialAssault;
            if (lordAssault != null)
            {
                return lordAssault.ships.All(x => x.Destroyed || !x.Spawned) ;
            }
            return false;
        }

        public static bool HasPassengerSeats(ShipBase ship)
        {
            return (ship.GetDirectlyHeldThings().ToList<Thing>().Count(x => x is Pawn && x.def.race.Humanlike) < ship.compShip.sProps.maxPassengers);
        }
    }
}
