using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace OHUShips
{
    [StaticConstructorOnStartup]
    public class WorldShip : WorldObject, IIncidentTarget, ITrader, ILoadReferenceable
    {
        public List<WorldShipData> WorldShipData = new List<WorldShipData>();
        private static readonly Texture2D SplitCommand = ContentFinder<Texture2D>.Get("UI/Commands/SplitCaravan", true);

        public WorldShipPather pather;
        public WorldShipTrader trader;

        private Material cachedMat;
        public override Material Material
        {
            get
            {
                if (this.cachedMat == null)
                {
                    this.cachedMat = MaterialPool.MatFrom(WorldShipData.FirstOrDefault().Ship.def.graphicData.texPath, ShaderDatabase.WorldOverlayCutout, WorldShipData[0].Ship.DrawColor, WorldMaterials.WorldObjectRenderQueue);
                }
                return cachedMat;
            }
        }

        public override Texture2D ExpandingIcon
        {
            get
            {
                if (this.WorldShipData.Count > 1)
                {
                    return DropShipUtility.movingFleet;
                }
                return DropShipUtility.movingShip;
            }
        }

        public WorldShip()
        {
            pather = new WorldShipPather(this);
            trader = new WorldShipTrader(this);
        }

        private string _label = "Ship";
        public override string Label
        {
            get
            {
                return _label;
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (this.pather.Moving)
                {
                    return this.pather.GetDrawPos();
                }
                return base.DrawPos;
            }
        }

        public IEnumerable<Pawn> AllPassengers
        {
            get
            {
                return this.WorldShipData.SelectMany(x => x.Passengers).ToList();
            }
        }

        public void Rename(string newName)
        {
            this._label = newName;
        }

        public override void Tick()
        {
            base.Tick();
            this.pather.PatherTick();
            if (this.IsTargeting)
            {
                GhostDrawer.DrawGhostThing(UI.MouseCell(), this.WorldShipData[0].Ship.Rotation, this.WorldShipData[0].Ship.def, null, new Color(0.5f, 1f, 0.6f, 0.4f), AltitudeLayer.Blueprint);
            }
            this.BurnFuel();
        }

        private float maxTravelingSpeed = -1;
        public float MaxTravelingSpeed
        {
            get
            {
                if (this.maxTravelingSpeed == -1)
                {
                    List<float> speedFactors = new List<float>();
                    foreach (WorldShipData data in this.WorldShipData)
                    {
                        speedFactors.Add(data.Ship.compShip.sProps.WorldMapTravelSpeedFactor);
                    }
                    float chosenFactor = Mathf.Min(speedFactors.ToArray());
                    maxTravelingSpeed = chosenFactor * 0.0000416f;
                }

                return maxTravelingSpeed;
            }
        }

        public bool IsPlayerControlled
        {
            get
            {
                return this.Faction == Faction.OfPlayer;
            }
        }

        #region "Incidents"

        private StoryState _storyState;
        public StoryState StoryState => _storyState;

        public GameConditionManager GameConditionManager => Find.World.GameConditionManager;

        public float PlayerWealthForStoryteller => 10000000f;

        public IEnumerable<Pawn> PlayerPawnsForStoryteller => this.WorldShipData[0].Passengers;

        public FloatRange IncidentPointsRandomFactorRange => new FloatRange(1000, 2000);

        public int ConstantRandSeed => 50000;

        #endregion

        private void BurnFuel()
        {
            if (this.pather.Moving)
            {
                foreach (WorldShipData data in this.WorldShipData)
                {
                    data.Ship.ConsumeFuel();
                    if (!data.Ship.refuelableComp.HasFuel && !data.Ship.Destroyed)
                    {
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipOutOfFuelCrash", data.Ship.ShipNick), MessageTypeDefOf.ThreatBig);
                        data.Ship.Destroy();
                        DropShipUtility.CurrentShipTracker.AllPlanetShips.Remove(data.Ship);
                    }
                }

                this.CheckFuelStatus();

            }
            this.WorldShipData.RemoveAll(x => x.Ship.Destroyed);
        }

        internal void Arrive(IntVec3 targetCell, ShipArrivalAction arrivalAction, PawnsArrivalModeDef mapArrivalMode)
        {
            if (this.IsPlayerControlled && arrivalAction != ShipArrivalAction.BombingRun)
            {
                Messages.Message("MessageShipsArrived".Translate(), this, MessageTypeDefOf.NeutralEvent);
            }

            if (arrivalAction == ShipArrivalAction.EnterMapAssault || arrivalAction == ShipArrivalAction.EnterMapFriendly)
            {
                MapParent parent = Find.WorldObjects.MapParentAt(this.Tile);
                if (parent != null)
                {
                    Map map = parent.Map;
                    if (map == null)
                    {

                        LongEventHandler.QueueLongEvent(delegate
                        {
                            MapGeneratorDef def = WorldShipUtility.GetMapGeneratorDefForObject(parent);
                            map = MapGenerator.GenerateMap(Find.World.info.initialMapSize, parent, MapGeneratorDefOf.Base_Faction);
                            targetCell = IntVec3.Zero;
                        }, "GeneratingMap", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap));
                    }
                    LongEventHandler.QueueLongEvent(delegate
                    {
                        TravelingShipsUtility.EnterMapWithShip(this, map, targetCell, arrivalAction, mapArrivalMode);
                    }, "SpawningColonists", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap));
                }
            }
            else if (arrivalAction == ShipArrivalAction.BombingRun)
            {
                if (BombingUtility.TryBombWorldTarget(this.Tile, this))
                {

                }
            }
        }

        private void CheckFuelStatus()
        {
            foreach (WorldShipData data in this.WorldShipData)
            {
                if (data.Ship.refuelableComp.FuelPercentOfMax == 0.05f)
                {
                    Find.TickManager.Pause();
                    Find.LetterStack.ReceiveLetter("PendingShipCrash".Translate(), TranslatorFormattedStringExtensions.Translate("PendingShipCrashDesc", data.Ship.ShipNick), LetterDefOf.NegativeEvent);
                }
            }
        }

        public void AddShip(ShipBase ship, bool justLeftTheMap)
        {
            if (!this.WorldShipData.Any(x => x.Ship == ship))
            {
                WorldShipData worldShipData = new WorldShipData(ship);
                foreach (Thing t in ship.GetDirectlyHeldThings())
                {
                    if (t is Pawn p)
                    {
                        p.ClearMind();
                        //if (!Find.WorldPawns.Contains(p))
                        //{
                        //    Find.WorldPawns.PassToWorld(p);
                        //}
                    }
                }
                this.WorldShipData.Add(worldShipData);
            }
            if (ship.fleetID > -1)
            {
                this._label = DropShipUtility.CurrentShipTracker.PlayerFleetManager[ship.fleetID];
            }
        }

        public bool ContainsPawn(Pawn p)
        {
            for (int i = 0; i < this.WorldShipData.Count; i++)
            {
                if (this.WorldShipData[i].Ship.GetDirectlyHeldThings().Contains(p))
                {
                    return true;
                }
            }
            return false;
        }

        public override void ExtraSelectionOverlaysOnGUI()
        {
            base.ExtraSelectionOverlaysOnGUI();
            if (this.IsPlayerControlled && this.pather.WorldPath != null)
            {
                this.pather.WorldPath.DrawPath(null);
            }
        }

        [DebuggerHidden]
        public override IEnumerable<Gizmo> GetGizmos()
        {
            for (int i = 0; i < this.AllComps.Count; i++)
            {
                foreach (Gizmo gizmo in this.AllComps[i].GetGizmos())
                {
                    yield return gizmo;
                }
            }
            if (this.IsPlayerControlled)
            {
                if (Find.WorldSelector.SingleSelectedObject == this)
                {
                    yield return WorldShipUtility.ShipTouchdownCommand(this, true);
                    yield return WorldShipUtility.ShipTouchdownCommand(this, false);

                    Command_Action command_Action = new Command_Action();
                    command_Action.defaultLabel = "CommandLaunchShip".Translate();
                    command_Action.defaultDesc = "CommandLaunchShipDesc".Translate();
                    command_Action.icon = DropShipUtility.LaunchSingleCommandTex;
                    command_Action.action = delegate
                    {
                        SoundDef.Named("ShipTakeoff_SuborbitalLaunch").PlayOneShotOnCamera();
                        this.IsTargeting = true;
                        this.WorldShipData.FirstOrDefault().Ship.StartChoosingDestination(false);
                    };
                    yield return command_Action;

                }



                SettlementBase Settlement = (Settlement)Find.WorldObjects.SettlementAt(this.Tile);
                if (Settlement != null && Settlement.CanTradeNow)
                {
                    yield return WorldShipUtility.TradeCommand(this);
                }

                if (this.pather.Moving)
                {
                    yield return new Command_Action
                    {
                        hotKey = KeyBindingDefOf.Misc1,
                        action = delegate
                        {
                            if (!this.pather.Moving)
                            {
                                return;
                            }
                            this.pather.ToggleCircling();
                        },
                        defaultDesc = "CommandWorldShipHoldDesc".Translate(2f.ToString("0.#"), 0.3f.ToStringPercent()),
                        icon = TexCommand.PauseCaravan,
                        defaultLabel = "CommandWorldShipHold".Translate()
                    };
                    yield return new Command_Action
                    {
                        hotKey = KeyBindingDefOf.Misc1,
                        action = delegate
                        {
                            if (!this.pather.Moving)
                            {
                                return;
                            }
                            this.pather.Halt();
                        },
                        defaultDesc = "CommandWorldShipHaltDesc".Translate(2f.ToString("0.#"), 0.3f.ToStringPercent()),
                        icon = TexCommand.PauseCaravan,
                        defaultLabel = "CommandWorldShipHalt".Translate()
                    };
                }
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look<WorldShipData>(ref this.WorldShipData, "WorldShipData", LookMode.Deep);
            Scribe_Deep.Look<WorldShipPather>(ref this.pather, "pather", new object[]
            {
                this
            });
            Scribe_Deep.Look<WorldShipTrader>(ref this.trader, "trader", new object[]
            {
                this
            });
            base.ExposeData();
        }


        public TraderKindDef TraderKind
        {
            get
            {
                List<Pawn> pawnsListForReading = this.AllPassengers.ToList();
                for (int i = 0; i < pawnsListForReading.Count; i++)
                {
                    Pawn pawn = pawnsListForReading[i];
                    if (pawn.TraderKind != null)
                    {
                        return pawn.TraderKind;
                    }
                }
                return null;
            }
        }

        public IEnumerable<Thing> Goods
        {
            get
            {
                yield return null;
            }
        }

        public int RandomPriceFactorSeed => 1;

        public string TraderName => this.WorldShipData[0].Ship.ShipNick;

        public bool CanTradeNow => true;

        public float TradePriceImprovementOffsetForPlayer => 1f;

        public bool IsTargeting { get; set; }

        public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
        {
            return trader.Goods;
        }

        public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            this.trader.GiveSoldThingToTrader(toGive, countToGive, playerNegotiator);
        }

        public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            this.trader.GiveSoldThingToPlayer(toGive, countToGive, playerNegotiator);
        }

        public void Launch(int targetTile, IntVec3 targetCell, ShipArrivalAction arrivalAction = ShipArrivalAction.StayOnWorldMap, PawnsArrivalModeDef pawnsArrivalMode = null)
        {
            this.pather.SetDestination(targetTile, targetCell, arrivalAction, pawnsArrivalMode);
        }
    }
}
