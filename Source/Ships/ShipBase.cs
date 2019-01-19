using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace OHUShips
{
    public class ShipBase : Building, IThingHolder
    {
        public bool FirstSpawned = true;

        public List<Building_ShipTurret> assignedTurrets = new List<Building_ShipTurret>();
        public Dictionary<WeaponSystem, bool> assignedSystemsToModify = new Dictionary<WeaponSystem, bool>();
        public List<WeaponSystemShipBomb> loadedBombs = new List<WeaponSystemShipBomb>();

        public Dictionary<ShipWeaponSlot, Building_ShipTurret> installedTurrets = new Dictionary<ShipWeaponSlot, Building_ShipTurret>();
        public Dictionary<ShipWeaponSlot, WeaponSystemShipBomb> Payload = new Dictionary<ShipWeaponSlot, WeaponSystemShipBomb>();
        public Dictionary<ShipWeaponSlot, Thing> weaponsToInstall = new Dictionary<ShipWeaponSlot, Thing>();
        public Dictionary<ShipWeaponSlot, Thing> weaponsToUninstall = new Dictionary<ShipWeaponSlot, Thing>();

        public bool shouldSpawnTurrets = false;
        //public bool shouldDeepSave = true;

        public List<Pawn> assignedNewPawns = new List<Pawn>();

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.innerContainer;
        }

        public float MassUsage
        {
            get
            {
                return CollectionsMassCalculator.MassUsage<Thing>(this.GetDirectlyHeldThings().ToList(), IgnorePawnsInventoryMode.DontIgnore, true, false) / this.compShip.sProps.maxCargo;
            }
        }

        public override Graphic Graphic
        {
            get
            {
                return GraphicDatabase.Get<Graphic_Single>(this.def.graphicData.texPath, ShaderDatabase.LoadShader(this.def.graphicData.shaderType.shaderPath), this.def.graphicData.drawSize, this.DrawColor, this.DrawColorTwo);
            }
        }


        public string ShipNick = "Ship";

        public ShipState shipState = ShipState.Stationary;

        private ThingOwner innerContainer;

        protected CompShip compShipCached;

        public CompShip compShip
        {
            get
            {
                if (this.compShipCached == null)
                {
                    this.compShipCached = this.TryGetComp<CompShip>();
                }
                return this.compShipCached;
            }
        }

        protected CompRefuelable refuelableCompCached;

        public CompRefuelable refuelableComp
        {
            get
            {
                if (this.refuelableCompCached == null)
                {
                    this.refuelableCompCached = this.TryGetComp<CompRefuelable>();
                }
                return this.refuelableCompCached;
            }
        }

        public int drawTickOffset = 0;

        private const int maxTimeToWait = 3000;

        private int timeWaited = 0;

        private int timeToLiftoff = 50;

        private bool NoneLeftBehind = false;

        private bool ShouldWait = false;

        public bool IsTargeting { get; set; }

        public bool keepShipReference;

        public int fleetID = -1;

        public bool LaunchAsFleet;

        public bool performBombingRun;

        public Map ParkingMap;

        public IntVec3 ParkingPosition;

        private WorldShip parentShipCached;

        public WorldShip parentWorldShip
        {
            get
            {
                if (this.parentShipCached == null)
                {

                    foreach (WorldShip ship in Find.WorldObjects.AllWorldObjects.FindAll(x => x is WorldShip))
                    {
                        if (ship.WorldShipData.FirstOrDefault(s => s.Ship == this) != null)
                        {
                            this.parentShipCached = ship;
                        }
                    }
                }
                return this.parentShipCached;
            }
        }

        public bool pilotPresent
        {
            get
            {
                if (this.parentShipCached != null)
                {
                    //return this.parentShipCached.PawnsListForReading.Count > 0;
                }
                return this.innerContainer.Any(x => x is Pawn && x.Faction == this.Faction && x.def.race.Humanlike);
            }
        }

        public bool ShouldSpawnFueled;

        public bool holdFire = true;

        public bool ActivatedLaunchSequence;

        private bool DeepsaveTurrets = false;

        public ShipBase()
        {
            this.innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }


        public ShipBase(bool isIncoming)
        {
            this.innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public ShipBase(bool isIncoming = false, bool shouldSpawnRefueled = false)
        {
            if (isIncoming)
            {
                this.shipState = ShipState.Incoming;
                this.drawTickOffset = this.compShip.sProps.TicksToImpact;
            }
            else
            {
                this.shipState = ShipState.Stationary;
            }
            this.ShouldSpawnFueled = shouldSpawnRefueled;
            this.innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public override void PostMake()
        {
            base.PostMake();
            this.InitiateShipProperties();
            this.RecolorShip();
        }

        public virtual void RecolorShip()
        {
            this.FirstSpawned = true;
        }

        public int MaxLaunchDistanceEverPossible(bool LaunchAsFleet, bool includeReturnFlight = false)
        {
            if (LaunchAsFleet && this.fleetID != -1)
            {
                List<ShipBase> fleetShips = DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID);

                ShipBase lowest = fleetShips.Aggregate((curMin, x) => (curMin == null || x.MaxLaunchDistanceEverPossible(false) < curMin.MaxLaunchDistanceEverPossible(false) ? x : curMin));
                return (int)((lowest.MaxShipFlightTicks * lowest.compShip.sProps.WorldMapTravelSpeedFactor * 0.0000416f) / 0.005F);
            }
            return (int)((this.MaxShipFlightTicks * this.compShip.sProps.WorldMapTravelSpeedFactor * 0.0000416f) / 0.005F);
        }

        public int MaxShipFlightTicks
        {
            get
            {
                float consumption = this.refuelableComp.Props.fuelConsumptionRate;
                float fuel = this.refuelableComp.Fuel;
                return (int)((fuel / consumption) * 60) - MapFlightTicks;
            }

        }

        private int MapFlightTicks
        {
            get
            {
                return this.compShip.sProps.TicksToImpact + this.compShip.sProps.TicksToDespawn;
            }
        }


        public bool ReadyForTakeoff
        {
            get
            {
                return this.pilotPresent && this.refuelableComp.HasFuel;
            }
        }


        private void InitiateShipProperties()
        {
            DropShipUtility.CurrentShipTracker.AllPlanetShips.Add(this);
            this.ShipNick = NameGenerator.GenerateName(RulePackDef.Named("NamerShipGeneric"));
            this.compShipCached = this.TryGetComp<CompShip>();
            if (this.compShip == null)
            {
                Log.Error("DropShip is missing " + nameof(CompProperties_Ship) + "/n Defaulting.");
                this.compShipCached = new CompShip();
                this.drawTickOffset = compShip.sProps.TicksToImpact;
            }
            if (this.installedTurrets.Count == 0)
            {
                this.InitiateInstalledTurrets();
            }
        }

        private void InitiateInstalledTurrets()
        {
            foreach (ShipWeaponSlot current in this.compShip.sProps.weaponSlots)
            {
                if (current.slotType == WeaponSystemType.LightCaliber)
                {

                    this.installedTurrets.Add(current, null);
                }
                if (current.slotType == WeaponSystemType.Bombing)
                {
                    this.Payload.Add(current, null);
                }
                if (this.assignedTurrets.Count > 0)
                {
                    Building_ShipTurret turret = this.assignedTurrets.Find(x => x.assignedSlotName == current.SlotName);
                    if (turret != null)
                    {
                        turret.AssignParentShip(this);
                        this.installedTurrets[current] = turret;
                    }
                }
                else
                {
                }
                if (this.loadedBombs.Count > 0)
                {
                    WeaponSystemShipBomb bomb = (WeaponSystemShipBomb)this.loadedBombs.First(x => x.assignedSlotName == current.SlotName);
                    if (bomb != null)
                    {
                        this.Payload[current] = bomb;
                    }
                }
                if (this.assignedSystemsToModify.Count > 0)
                {
                    KeyValuePair<WeaponSystem, bool> entry = this.assignedSystemsToModify.First(x => x.Key.assignedSlotName == current.SlotName);
                    this.TryModifyWeaponSystem(current, entry.Key, entry.Value);
                }
            }
        }

        public bool TryModifyWeaponSystem(ShipWeaponSlot slot, Thing system, bool AddForInstalling = true)
        {
            if (AddForInstalling)
            {
                if (this.weaponsToInstall.ContainsKey(slot))
                {
                    this.weaponsToInstall.Remove(slot);
                }
                this.weaponsToInstall.Add(slot, system);
                return true;
            }
            else
            {
                if (this.weaponsToUninstall.ContainsKey(slot))
                {
                    this.weaponsToUninstall.Remove(slot);
                }
                this.weaponsToUninstall.Add(slot, system);
                return true;
            }
        }


        public override void Tick()
        {
            base.Tick();
            if (Find.Targeter.IsTargeting || Find.WorldTargeter.IsTargeting)
            {
                if (this.IsTargeting)
                {
                    GhostDrawer.DrawGhostThing(UI.MouseCell(), this.Rotation, this.def, null, new Color(0.5f, 1f, 0.6f, 0.4f), AltitudeLayer.Blueprint);
                }
            }
            else
            {
                this.IsTargeting = false;
            }
            for (int i = 0; i < DropShipUtility.AllPawnsInShip(this).Count; i++)
            {
                Pawn pawn = DropShipUtility.AllPawnsInShip(this)[i];
                float num = 0.6f;
                num = 0.7f * num + 0.3f * num;
                pawn.needs.rest.TickResting(num);
            }

            if (this.shipState == ShipState.Incoming)
            {
                this.drawTickOffset--;
                if (this.drawTickOffset <= 0)
                {
                    this.drawTickOffset = 0;
                }
                this.ConsumeFuel();
            }

            if (ReadyForTakeoff && ActivatedLaunchSequence)
            {
                this.timeToLiftoff--;
                if (this.ShouldWait)
                {
                    int num = GenDate.TicksPerHour;
                    this.timeToLiftoff += num;
                    this.timeWaited += num;
                    if (this.timeWaited >= maxTimeToWait)
                    {
                        this.ShouldWait = false;
                        this.timeToLiftoff = 0;
                    }
                }
                if (this.timeToLiftoff == 0)
                {
                    this.shipState = ShipState.Outgoing;
                    this.ActivatedLaunchSequence = false;
                    this.timeWaited = 0;
                }
            }

            if (shipState == ShipState.Outgoing)
            {
                this.drawTickOffset++;
                this.ConsumeFuel();
                if (this.Spawned)
                {
                    ShipBase_Traveling travelingShip = new ShipBase_Traveling(this, this.LaunchAsFleet, ShipArrivalAction.StayOnWorldMap);

                    Map curMap = this.Map;
                    IntVec3 curPos = this.Position;

                    //GenSpawn.Spawn(travelingShip, curPos, curMap, this.Rotation, false);
                    this.DeSpawn();
                    //Log.Message(GenAdj.CellsOccupiedBy(curPos, Rot4.North, def.Size).Count().ToString());
                    //foreach (IntVec3 current in GenAdj.CellsOccupiedBy(curPos, Rot4.North, def.Size))
                    //{

                    //    Log.Message("A: " + curMap.thingGrid.ThingsAt(current).ToList<Thing>().Count.ToString());
                    //    foreach (Thing current2 in curMap.thingGrid.ThingsAt(current).ToList<Thing>())
                    //    {
                    //        if (current2.def == null) Log.Message("NoDef?");
                    //        Log.Message("B");
                    //        if (GenSpawn.SpawningWipes(def, current2.def))
                    //        {
                    //            Log.Message("Destroyng:" + current2.Label);
                    //            current2.Destroy(DestroyMode.Vanish);
                    //        }
                    //    }
                    //}
                    GenSpawn.Spawn(travelingShip, curPos, curMap, this.Rotation, WipeMode.Vanish, false);
                }
            }
        }

        internal void ConsumeFuel()
        {
            this.refuelableComp.ConsumeFuel(this.refuelableComp.Props.fuelConsumptionRate / 60f);
        }

        public void TryLaunch(RimWorld.Planet.GlobalTargetInfo target, PawnsArrivalModeDef arriveMode, ShipArrivalAction arrivalAction, bool launchedAsSingleShip = false)
        {
            this.timeToLiftoff = 0;
            if (this.parentWorldShip == null)
            {
                this.shipState = ShipState.Outgoing;
                ShipBase_Traveling travelingShip = new ShipBase_Traveling(this, target, arriveMode, arrivalAction);
                Map curMap = this.Map;
                IntVec3 curPos = this.Position;
                this.DeSpawn();
                GenSpawn.Spawn(travelingShip, curPos, curMap, this.Rotation, WipeMode.Vanish);
                if (this.LaunchAsFleet)
                {
                    foreach (ShipBase current in DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID))
                    {
                        if (current != this && current.Spawned)
                        {
                            current.shipState = ShipState.Outgoing;
                            ShipBase_Traveling travelingShip2 = new ShipBase_Traveling(current, target, arriveMode, arrivalAction);
                            Map shipMap = current.Map;
                            IntVec3 shipPos = current.Position;
                            current.DeSpawn();
                            GenSpawn.Spawn(travelingShip2, shipPos, shipMap, current.Rotation, WipeMode.Vanish);
                        }
                    }
                }
            }
            else
            {
                //      Find.WorldSelector.Select(parentLandedShip);
                //TravelingShipsUtility.LaunchLandedFleet(this.parentWorldShip, target.Tile, target.Cell, arriveMode, arrivalAction);
                this.parentWorldShip.Launch(target.Tile, target.Cell, arrivalAction, arriveMode);
                //this.parentShipCached = null;
                //Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.World, false);
            }
        }


        public override Vector3 DrawPos
        {
            get
            {
                return DropShipUtility.DrawPosAt(this, this.drawTickOffset);
            }
        }

        public override void Draw()
        {
            base.Draw();
            DropShipUtility.DrawDropSpotShadow(this, this.drawTickOffset);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode == DestroyMode.KillFinalize)
            {
                this.ShipUnload(true);
            }
            if (mode == DestroyMode.Deconstruct)
            {
                this.ShipUnload(false);
            }
            if (mode == DestroyMode.Vanish)
            {
            }
            foreach (Building_ShipTurret current in this.assignedTurrets)
            {
                current.Destroy(mode);
            }

            DropShipUtility.CurrentShipTracker.RemoveShip(this);
            base.Destroy(mode);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            this.compShip.TryRemoveLord(this.Map);
            base.DeSpawn();
            this.DeepsaveTurrets = true;
            //        this.SavePotentialWorldPawns();
            List<ShipWeaponSlot> slotsToRemove = new List<ShipWeaponSlot>();
            foreach (KeyValuePair<ShipWeaponSlot, Building_ShipTurret> current in this.installedTurrets)
            {
                if (current.Value != null)
                {
                    if (!current.Value.Destroyed)
                    {
                        current.Value.DeSpawn();
                    }
                    else
                    {
                        slotsToRemove.Add(current.Key);
                    }
                }
            }
            for (int i = 0; i < slotsToRemove.Count; i++)
            {
                this.installedTurrets[slotsToRemove[i]] = null;
            }
        }

        public void ShipUnload(bool wasDestroyed = false, bool dropPawns = true, bool dropitems = false)
        {
            List<Thing> allCargo = new List<Thing>();
            allCargo.AddRange(this.GetDirectlyHeldThings());
            for (int i = 0; i < allCargo.Count; i++)
            {
                Thing thing = allCargo[i];
                Thing thing2;
                if (wasDestroyed && Rand.Range(0, 1f) < 0.3f)
                {
                    thing.Destroy(DestroyMode.KillFinalize);
                }
                else
                {
                    Pawn pawn1 = thing as Pawn;
                    if ((pawn1 != null && dropPawns && !pawn1.IsPrisoner && pawn1.def.race.Humanlike) || (dropitems && thing.GetType() != typeof(Pawn)))
                    {
                        if (this.GetDirectlyHeldThings().TryDrop(thing, base.Position, this.Map, ThingPlaceMode.Near, out thing2, delegate (Thing placedThing, int count)
                         {
                             if (Find.TickManager.TicksGame < 1200 && TutorSystem.TutorialMode && placedThing.def.category == ThingCategory.Item)
                             {
                                 Find.TutorialState.AddStartingItem(placedThing);
                             }
                         }))
                        {

                            Pawn pawn2 = thing2 as Pawn;
                            if (pawn2 != null)
                            {
                                if (pawn2.RaceProps.Humanlike)
                                {
                                    TaleRecorder.RecordTale(TaleDefOf.LandedInPod, new object[]
                                    {
                            pawn2
                                    });
                                }
                                if (pawn2.IsColonist && pawn2.Spawned && !base.Map.IsPlayerHome)
                                {
                                    pawn2.drafter.Drafted = true;
                                }
                            }
                        }
                    }
                    else if (dropitems && thing.GetType() != typeof(Pawn))
                    {
                        this.GetDirectlyHeldThings().TryDrop(thing, ThingPlaceMode.Near, out thing);
                    }
                }
            }

            SoundDef.Named("DropPodOpen").PlayOneShot(new TargetInfo(base.Position, base.Map, false));
        }

        public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (!this.Accepts(thing))
            {
                return false;
            }
            if (thing is Pawn)
            {
                Pawn pawn = thing as Pawn;
                if (pawn.def.race.Humanlike)
                {
                    if (!DropShipUtility.HasPassengerSeats(this))
                    {
                        Messages.Message("MessagePassengersFull".Translate(new object[] { pawn.Name, this.ShipNick }), this, MessageTypeDefOf.RejectInput);
                        return false;
                    }
                    if (pawn.Spawned)
                    {
                        pawn.DeSpawn();
                    }
                    if (!this.innerContainer.Contains(pawn))
                    {
                        //pawn.InContainerEnclosed
                        this.innerContainer.TryAdd(pawn, 1, false);
                    }

                }
                else
                {
                    if (this.innerContainer.TryAdd(thing))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            bool flag;
            if (thing.holdingOwner != null)
            {

                flag = thing.holdingOwner.TryTransferToContainer(thing, this.innerContainer);

            }
            else
            {
                flag = this.innerContainer.TryAdd(thing.SplitOff(thing.stackCount), true);
            }
            if (flag)
            {
                return true;
            }
            return false;
        }

        public void PrepareForLaunchIn(int ticksToLiftoff, bool noOneLeftBehind = false)
        {
            this.ActivatedLaunchSequence = true;
            this.timeToLiftoff = ticksToLiftoff;
            this.NoneLeftBehind = noOneLeftBehind;
        }


        public void WaitForLordPassengers(List<Pawn> potentialPassengers, bool noneLeftBehind = false)
        {
            int passengersPresent = 0;
            for (int i = 0; i > this.innerContainer.Count; i++)
            {
                if (potentialPassengers.Any(x => this.innerContainer[i] == x))
                {
                    passengersPresent++;
                }
            }
            if (noneLeftBehind && passengersPresent < potentialPassengers.Count)
            {
                this.ShouldWait = true;
            }
            else if (passengersPresent < potentialPassengers.Count)
            {
                this.ShouldWait = true;
            }
            else
            {
                ShouldWait = false;
            }
        }


        public virtual bool Accepts(Thing thing)
        {
            return this.innerContainer.CanAcceptAnyOf(thing);
        }

        public Map GetMap()
        {
            return this.Map;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.parentShipCached = null;
            //this.shouldDeepSave = true;
            this.DeepsaveTurrets = false;
            if (shouldSpawnTurrets)
            {
                foreach (KeyValuePair<ShipWeaponSlot, Building_ShipTurret> current in this.installedTurrets)
                {
                    if (current.Value != null && !current.Value.Spawned)
                    {
                        IntVec3 drawLoc = this.Position + DropShipUtility.AdjustedIntVecForShip(this, current.Key.turretPosOffset);
                        GenSpawn.Spawn(current.Value, drawLoc, this.Map);
                    }
                }
            }
            this.shouldSpawnTurrets = false;
            if (shipState == ShipState.Incoming)
            {
                SoundDef.Named("ShipTakeoff_SuborbitalLaunch").PlayOneShotOnCamera();
            }

            if (this.ShouldSpawnFueled)
            {
                this.refuelableComp.Refuel(800);
                this.ShouldSpawnFueled = false;
            }
            DropShipUtility.InitializeDropShipSpawn(this);
            this.FirstSpawned = false;
        }

        public IntVec3 GetPosition()
        {
            return this.Position;
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            IEnumerator<FloatMenuOption> enumerator = base.GetFloatMenuOptions(selPawn).GetEnumerator();
            while (enumerator.MoveNext())
            {
                FloatMenuOption current = enumerator.Current;
                yield return current;
            }
            Action action = delegate
            {
                if (selPawn.CanReach(this, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    Job job = new Job(ShipNamespaceDefOfs.EnterShip, this);
                    selPawn.jobs.TryTakeOrderedJob(job);
                }
            };
            if (DropShipUtility.AllPawnsInShip(this).Count < this.compShip.sProps.maxPassengers + 1)
            {
                yield return new FloatMenuOption("EnterShip".Translate(), action, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
            else
            {
                yield return new FloatMenuOption("ShipPassengersFull".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
            }
        }


        public bool TryInstallTurret(CompShipWeapon comp)
        {
            if (comp.SProps.TurretToInstall != null)
            {
                ShipWeaponSlot slot = comp.slotToInstall;
                Building_ShipTurret turret = (Building_ShipTurret)ThingMaker.MakeThing(comp.SProps.TurretToInstall, null);
                turret.installedByWeaponSystem = comp.parent.def;
                this.installedTurrets[slot] = turret;
                turret.AssignParentShip(this);
                turret.assignedSlotName = slot.SlotName;
                turret.SetFactionDirect(this.Faction);
                if (slot.turretMinSize.x != turret.def.size.x)
                {
                    //           turret.def.size.x = slot.turretMinSize.x;
                }
                if (slot.turretMinSize.z != turret.def.size.z)
                {
                    //            turret.def.size.z = slot.turretMinSize.z;
                }
                IntVec3 drawLoc = this.Position + DropShipUtility.AdjustedIntVecForShip(this, slot.turretPosOffset);
                if (!turret.Spawned)
                {
                    GenSpawn.Spawn(turret, drawLoc, this.Map);
                }
                this.assignedTurrets.Add(turret);
                return true;
            }
            return false;
        }
        public bool TryInstallPayload(WeaponSystemShipBomb bomb, CompShipWeapon comp)
        {
            if (comp.SProps.PayloadToInstall != null)
            {
                ShipWeaponSlot slot = comp.slotToInstall;
                this.loadedBombs.Add(bomb);
                this.Payload[slot] = bomb;
                return true;
            }
            return false;
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerator<Gizmo> enumerator = base.GetGizmos().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Gizmo current = enumerator.Current;
                yield return current;
            }
            if (this.Faction == Faction.OfPlayer)
            {

                if (this.ReadyForTakeoff)
                {
                    Command_Action command_Action = new Command_Action();
                    command_Action.defaultLabel = "CommandLaunchShip".Translate();
                    command_Action.defaultDesc = "CommandLaunchShipDesc".Translate();
                    command_Action.icon = DropShipUtility.LaunchSingleCommandTex;
                    command_Action.action = delegate
                    {
                        SoundDef.Named("ShipTakeoff_SuborbitalLaunch").PlayOneShotOnCamera();
                        this.LaunchAsFleet = false;
                        this.StartChoosingDestination(this.LaunchAsFleet);
                    };
                    yield return command_Action;

                    if (this.fleetID != -1)
                    {

                        Command_Action command_Action3 = new Command_Action();
                        command_Action3.defaultLabel = "CommandLaunchFleet".Translate();
                        command_Action3.defaultDesc = "CommandLaunchFleetDesc".Translate();
                        command_Action3.icon = DropShipUtility.LaunchFleetCommandTex;
                        command_Action3.action = delegate
                        {
                            SoundDef.Named("ShipTakeoff_SuborbitalLaunch").PlayOneShotOnCamera();
                            this.LaunchAsFleet = true;
                            this.StartChoosingDestination(this.LaunchAsFleet);
                        };
                        if (DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID).Any(x => !x.ReadyForTakeoff))
                        {
                            command_Action3.Disable("CommandLaunchFleetFailDueToNotReady".Translate());
                        }

                        yield return command_Action3;
                    }
                }
                {
                    Command_Action command_Action2 = new Command_Action();
                    command_Action2.defaultLabel = "CommandLoadShipCargo".Translate();
                    command_Action2.defaultDesc = "CommandLoadShipCargoDesc".Translate();
                    command_Action2.icon = DropShipUtility.LoadCommandTex;
                    command_Action2.action = delegate
                    {
                        Find.WindowStack.Add(new Dialog_LoadShip(this.Map, this));
                    };
                    yield return command_Action2;
                }
                {
                    Command_Action command_Action3 = new Command_Action();
                    command_Action3.defaultLabel = "CommandSetParkingPosition".Translate();
                    command_Action3.defaultDesc = "CommandSetParkingPositionDesc".Translate();
                    command_Action3.icon = DropShipUtility.ParkingSingle;
                    command_Action3.action = delegate
                    {
                        this.ParkingMap = this.Map;
                        this.ParkingPosition = this.Position;

                    };
                    yield return command_Action3;
                }
                if (this.ParkingMap != null && this.ReadyForTakeoff)
                {
                    this.LaunchAsFleet = true;
                    Command_Action command_Action4 = new Command_Action();
                    command_Action4.defaultLabel = "CommandTravelParkingPosition".Translate();
                    command_Action4.defaultDesc = "CommandTravelParkingPositionDesc".Translate();
                    command_Action4.icon = DropShipUtility.ReturnParkingSingle;
                    command_Action4.action = delegate
                    {
                        this.TryLaunch(new GlobalTargetInfo(this.ParkingPosition, this.ParkingMap), PawnsArrivalModeDefOf.CenterDrop, ShipArrivalAction.EnterMapFriendly, false);
                    };
                    if (Find.WorldGrid.ApproxDistanceInTiles(this.Map.Tile, this.ParkingMap.Tile) > this.MaxLaunchDistanceEverPossible(false, false))
                    {
                        command_Action4.Disable("CommandParkingPositionTooFar".Translate());
                    }
                    yield return command_Action4;
                }

                if (this.ParkingMap != null && !DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID).Any(x => x.ParkingMap != null || !x.ReadyForTakeoff))
                {
                    Command_Action command_Action5 = new Command_Action();
                    command_Action5.defaultLabel = "CommandTravelParkingPositionFleet".Translate();
                    command_Action5.defaultDesc = "CommandTravelParkingPositionFleetDesc".Translate();
                    command_Action5.icon = DropShipUtility.ReturnParkingFleet;
                    command_Action5.action = delegate
                    {
                        foreach (ShipBase ship in DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID))
                        {
                            ship.TryLaunch(new GlobalTargetInfo(ship.ParkingPosition, ship.ParkingMap), PawnsArrivalModeDefOf.CenterDrop, ShipArrivalAction.EnterMapFriendly, false);
                        }

                    };
                    yield return command_Action5;
                }
            }

        }

        public void SavePotentialWorldPawns()
        {
            List<Pawn> tmp = this.innerContainer.Where(x => x is Pawn) as List<Pawn>;

            foreach (Pawn current in tmp)
            {
                Find.WorldPawns.PassToWorld(current, RimWorld.Planet.PawnDiscardDecideMode.Decide);
            }

        }

        public void StartChoosingDestination(bool launchAsFleet)
        {
            this.LaunchAsFleet = launchAsFleet;
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(this));
            Find.WorldSelector.ClearSelection();
            int tile;
            if (this.parentWorldShip != null)
            {
                tile = this.parentWorldShip.Tile;
            }
            else
            {
                tile = this.Map.Tile;
            }
            Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, DropShipUtility.TargeterShipAttachment, true, delegate
            {
                this.DrawFleetLaunchRadii(launchAsFleet, tile);
            }, delegate (GlobalTargetInfo target)
            {
                if (!target.IsValid)
                {
                    return null;
                }
                int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);
                if (num <= this.MaxLaunchDistanceEverPossible(this.LaunchAsFleet))
                {
                    return null;
                }
                if (num > this.MaxLaunchDistanceEverPossible(this.LaunchAsFleet))
                {
                    return "TransportPodDestinationBeyondMaximumRange".Translate();
                }
                return "TransportPodNotEnoughFuel".Translate();
            });

        }

        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            if (this.parentWorldShip != null)
            {
                this.parentWorldShip.IsTargeting = true;
            }
            this.IsTargeting = true;
            int tile;
            if (this.parentWorldShip != null)
            {
                tile = this.parentWorldShip.Tile;
            }
            else
            {
                tile = this.Map.Tile;
            }
            bool canBomb = true;
            if (!target.IsValid)
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (this.LaunchAsFleet)
            {
                List<int> distances = new List<int>();
                for (int i = 0; i < DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID).Count; i++)
                {
                    ShipBase ship = DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID)[i];
                    if (ship.compShip.cargoLoadingActive)
                    {
                        Messages.Message("MessageFleetLaunchImpossible".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                    int num = (Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile));
                    if (num > ship.MaxLaunchDistanceEverPossible(true))
                    {
                        Messages.Message("MessageFleetLaunchImpossible".Translate(), MessageTypeDefOf.RejectInput);
                        return false;
                    }
                    if (!(2 * num > ship.MaxLaunchDistanceEverPossible(true)))
                    {
                        canBomb = false;
                    }
                }
            }
            else
            {
                if (target.IsMapTarget)
                {
                    if (target.Map.listerThings.AllThings.Where(x => x is ShipBase_Traveling || x.GetType().IsAssignableFrom(typeof(ShipBase))).Any(y => GenAdj.OccupiedRect(y).Overlaps(GenAdj.OccupiedRect(this)))) //y.Position.InHorDistOf(target.Cell, Math.Max(y.def.size.x, y.def.size.z))))
                    {
                        SoundDefOf.ClickReject.PlayOneShotOnCamera();
                        return false;
                    }
                }

                int num = Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile);

                if (num > this.MaxLaunchDistanceEverPossible(this.LaunchAsFleet))
                {
                    Messages.Message("MessageTransportPodsDestinationIsTooFar".Translate(new object[]
                    {
                    CompLaunchable.FuelNeededToLaunchAtDist((float)num).ToString("0.#")
                    }), MessageTypeDefOf.RejectInput);
                    return false;
                }
                if (!(2 * num > this.MaxLaunchDistanceEverPossible(true)))
                {
                    canBomb = false;
                }
            }

            MapParent mapParent = target.WorldObject as MapParent;
            if (mapParent != null && mapParent.HasMap)
            {
                Map myMap = this.Map;
                Map map = mapParent.Map;
                Current.Game.CurrentMap = map;
                Targeter targeter = Find.Targeter;
                Action actionWhenFinished = delegate
                {
                    if (Find.Maps.Contains(myMap))
                    {
                        Current.Game.CurrentMap = myMap;
                    }
                };
                targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), delegate (LocalTargetInfo x)
                {
                    if (!this.ReadyForTakeoff || (this.LaunchAsFleet && DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID).Any(s => !s.ReadyForTakeoff)))
                    {
                        return;
                    }
                    this.TryLaunch(x.ToGlobalTargetInfo(map), PawnsArrivalModeDefOf.EdgeDrop, ShipArrivalAction.EnterMapFriendly);
                }, null, actionWhenFinished, DropShipUtility.TargeterShipAttachment);
                return true;
            }

            if (target.WorldObject is Settlement || target.WorldObject is Site)
            {
                Find.WorldTargeter.closeWorldTabWhenFinished = false;
                MapParent localMapParent = target.WorldObject as MapParent;
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                if (!target.WorldObject.Faction.HostileTo(Faction.OfPlayer))
                {
                    list.Add(new FloatMenuOption("VisitSettlement".Translate(new object[]
                    {
                        target.WorldObject.Label
                    }), delegate
                    {
                        if (!this.ReadyForTakeoff || this.LaunchAsFleet && DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID).Any(s => !s.ReadyForTakeoff))
                        {
                            return;
                        }
                        this.TryLaunch(target, PawnsArrivalModeDefOf.EdgeDrop, ShipArrivalAction.StayOnWorldMap);
                        CameraJumper.TryHideWorld();
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                list.Add(new FloatMenuOption("DropAtEdge".Translate(), delegate
                {
                    if (!this.ReadyForTakeoff || this.LaunchAsFleet && DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID).Any(s => !s.ReadyForTakeoff))
                    {
                        return;
                    }
                    this.TryLaunch(target, PawnsArrivalModeDefOf.EdgeDrop, ShipArrivalAction.EnterMapFriendly);
                    CameraJumper.TryHideWorld();
                }, MenuOptionPriority.Default, null, null, 0f, null, null));
                //list.Add(new FloatMenuOption("DropInCenter".Translate(), delegate
                //{
                //    if (!this.ReadyForTakeoff || this.LaunchAsFleet && DropShipUtility.currentShipTracker.ShipsInFleet(this.fleetID).Any(s => !s.ReadyForTakeoff))
                //    {
                //        return;
                //    }
                //    this.TryLaunch(target, PawnsArrivalModeDef.CenterDrop, TravelingShipArrivalAction.EnterMapFriendly);
                //    CameraJumper.TryHideWorld();
                //}, MenuOptionPriority.Default, null, null, 0f, null, null));

                list.Add(new FloatMenuOption("AttackSettlementAerial".Translate(), delegate
                {
                    if (!this.ReadyForTakeoff || this.LaunchAsFleet && DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID).Any(s => !s.ReadyForTakeoff))
                    {
                        return;
                    }
                    this.TryLaunch(target, PawnsArrivalModeDefOf.CenterDrop, ShipArrivalAction.EnterMapAssault);
                    CameraJumper.TryHideWorld();
                }, MenuOptionPriority.Default, null, null, 0f, null, null));


                if (canBomb && (DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID).Any(x => x.installedTurrets.Any(y => y.Key.slotType == WeaponSystemType.Bombing && y.Value != null))) || this.loadedBombs.Any())
                {
                    list.Add(new FloatMenuOption("BombSettlement".Translate(), delegate
                    {
                        if (!this.ReadyForTakeoff || this.LaunchAsFleet && DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID).Any(s => !s.ReadyForTakeoff))
                        {
                            return;
                        }
                        this.performBombingRun = true;
                        this.TryLaunch(target, PawnsArrivalModeDefOf.CenterDrop, ShipArrivalAction.BombingRun);
                        CameraJumper.TryHideWorld();
                    }, MenuOptionPriority.Default, null, null, 0f, null, null));
                }


                Find.WindowStack.Add(new FloatMenu(list));
                return true;
            }
            if (Find.World.Impassable(target.Tile))
            {
                Messages.Message("MessageTransportPodsDestinationIsInvalid".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            this.TryLaunch(target, PawnsArrivalModeDefOf.EdgeDrop, ShipArrivalAction.StayOnWorldMap);
            return true;
        }

        private void DrawFleetLaunchRadii(bool launchAsFleet, int tile)
        {
            GenDraw.DrawWorldRadiusRing(tile, this.MaxLaunchDistanceEverPossible(launchAsFleet));
            GenDraw.DrawWorldRadiusRing(tile, (int)(this.MaxLaunchDistanceEverPossible(launchAsFleet) * 0.48f));
            if (launchAsFleet)
            {
                foreach (ShipBase ship in DropShipUtility.CurrentShipTracker.ShipsInFleet(this.fleetID))
                {
                    GenDraw.DrawWorldRadiusRing(tile, ship.MaxLaunchDistanceEverPossible(launchAsFleet));
                    GenDraw.DrawWorldRadiusRing(tile, (int)(ship.MaxLaunchDistanceEverPossible(launchAsFleet) * 0.48f));
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref this.FirstSpawned, "FirstSpawned", false, false);
            Scribe_Values.Look<bool>(ref this.ActivatedLaunchSequence, "ActivatedLaunchSequence", false, false);
            Scribe_Values.Look<bool>(ref this.ShouldWait, "ShouldWait", false, false);
            Scribe_Values.Look<bool>(ref this.NoneLeftBehind, "NoneLeftBehind", false, false);
            Scribe_Values.Look<bool>(ref this.keepShipReference, "keepShipReference", false, false);
            Scribe_Values.Look<bool>(ref this.shouldSpawnTurrets, "shouldSpawnTurrets", false, false);
            //Scribe_Values.Look<bool>(ref this.shouldDeepSave, "shouldDeepSave", true, false);
            Scribe_Values.Look<string>(ref this.ShipNick, "ShipNick", "Ship", false);
            Scribe_Values.Look<ShipState>(ref this.shipState, "shipState", ShipState.Stationary, false);
            Scribe_Values.Look<int>(ref this.timeToLiftoff, "timeToLiftoff", 200, false);
            Scribe_Values.Look<int>(ref this.drawTickOffset, "drawTickOffset", 0, false);
            Scribe_Values.Look<int>(ref this.timeWaited, "timeWaited", 200, false);


            Scribe_References.Look(ref this.ParkingMap, "ParkingMap");
            Scribe_Values.Look<IntVec3>(ref this.ParkingPosition, "ParkingPosition", IntVec3.Zero, false);



            Scribe_Values.Look<bool>(ref this.DeepsaveTurrets, "DeepsaveTurrets", false, false);
            this.assignedTurrets.RemoveAll(x => x == null);
            if (this.DeepsaveTurrets)
            {
                Scribe_Collections.Look<Building_ShipTurret>(ref this.assignedTurrets, "assignedTurrets", LookMode.Deep, new object[0]);
            }
            else
            {
                Scribe_Collections.Look<Building_ShipTurret>(ref this.assignedTurrets, "assignedTurrets", LookMode.Reference, new object[0]);
            }


            Scribe_Collections.Look<WeaponSystemShipBomb>(ref this.loadedBombs, "loadedBombs", LookMode.Reference, new object[0]);
            if (this.assignedSystemsToModify.Count > 0)
            {
                Scribe_Collections.Look<WeaponSystem, bool>(ref this.assignedSystemsToModify, "assignedSystemsToModify", LookMode.Reference, LookMode.Value);
            }

            Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", new object[]
            {
             this
            });

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.InitiateInstalledTurrets();
            }
        }
    }
}
