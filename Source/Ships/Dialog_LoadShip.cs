﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse.Sound;
using Verse.AI.Group;
using Verse.AI;

namespace OHUShips
{
    public class Dialog_LoadShip : Window
    {
        private enum Tab
        {
            Pawns,
            Items
        }

        private Map map;

        private ShipBase ship;
        
        private List<TransferableOneWay> transferables;

        private TransferableOneWayWidget pawnsTransfer;

        private TransferableOneWayWidget itemsTransfer;

        private Dialog_LoadShip.Tab tab;

        private float lastMassFlashTime = -9999f;

        private bool massUsageDirty = true;

        private int numHaulers;

        private float cachedMassUsage;

        private bool caravanMassUsageDirty = true;

        private float cachedCaravanMassUsage;

        private bool caravanMassCapacityDirty = true;

        private float cachedCaravanMassCapacity;

        private string cachedCaravanMassCapacityExplanation;

        private bool tilesPerDayDirty = true;

        private float cachedTilesPerDay;

        private string cachedTilesPerDayExplanation;

        private bool daysWorthOfFoodDirty = true;

        private Pair<float, float> cachedDaysWorthOfFood;

        private bool foragedFoodPerDayDirty = true;

        private Pair<ThingDef, float> cachedForagedFoodPerDay;

        private string cachedForagedFoodPerDayExplanation;

        private bool visibilityDirty = true;

        private float cachedVisibility;

        private string cachedVisibilityExplanation;

        private const float TitleRectHeight = 35f;

        private const float BottomAreaHeight = 55f;

        private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);

        private static List<TabRecord> tabsList = new List<TabRecord>();

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1024f, (float)UI.screenHeight);
            }
        }

        protected override float Margin
        {
            get
            {
                return 0f;
            }
        }

        private float MassCapacity
        {
            get
            {
                return this.ship.compShip.sProps.maxCargo;
            }
        }

        //private float CaravanMassCapacity
        //{
        //    get
        //    {
        //        if (this.caravanMassCapacityDirty)
        //        {
        //            this.caravanMassCapacityDirty = false;
        //            StringBuilder stringBuilder = new StringBuilder();
        //            this.cachedCaravanMassCapacity = CollectionsMassCalculator.CapacityTransferables(this.transferables, stringBuilder);
        //            this.cachedCaravanMassCapacityExplanation = stringBuilder.ToString();
        //        }
        //        return this.cachedCaravanMassCapacity;
        //    }
        //}

        private int MaxPassengerSeats
        {
            get
            {
                return this.ship.compShip.sProps.maxPassengers;
            }
        }

        private int CurrentPassengerCount
        {
            get
            {
                return this.transferables.Where(x => x.AnyThing.def.race != null && x.AnyThing.def.race.Humanlike && x.CountToTransfer > 0).Count();
            }
        }

        private string TransportersLabel
        {
            get
            {
                return this.ship.ShipNick;
            }
        }

        private string TransportersLabelCap
        {
            get
            {
                return this.TransportersLabel.CapitalizeFirst();
            }
        }

        private BiomeDef Biome
        {
            get
            {
                return this.map.Biome;
            }
        }

        private float MassUsage
        {
            get
            {
                if (this.massUsageDirty)
                {
                    this.massUsageDirty = false;
                    this.cachedMassUsage = this.ship.GetDirectlyHeldThings().Sum(c => c.stackCount * c.def.BaseMass) + CollectionsMassCalculator.MassUsageTransferables(this.transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true, false);
                }
                return this.cachedMassUsage;
            }
        }

        //public float CaravanMassUsage
        //{
        //    get
        //    {
        //        if (this.caravanMassUsageDirty)
        //        {
        //            this.caravanMassUsageDirty = false;
        //            this.cachedCaravanMassUsage = CollectionsMassCalculator.MassUsageTransferables(this.transferables, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, false, false);
        //        }
        //        return this.cachedCaravanMassUsage;
        //    }
        //}

        private float TilesPerDay
        {
            get
            {
                if (this.tilesPerDayDirty)
                {
                    this.tilesPerDayDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    var tilesPerDay = this.ship.compShip.sProps.WorldMapTravelSpeedFactor * 24;
                    this.cachedTilesPerDay = tilesPerDay;
                    stringBuilder.Append(tilesPerDay.ToString());
                    this.cachedTilesPerDayExplanation = stringBuilder.ToString();
                }
                return this.cachedTilesPerDay;
            }
        }

        private Pair<float, float> DaysWorthOfFood
        {
            get
            {
                if (this.daysWorthOfFoodDirty)
                {
                    this.daysWorthOfFoodDirty = false;
                    float first = DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(this.transferables, this.map.Tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, Faction.OfPlayer, null, 0f, 3300);
                    this.cachedDaysWorthOfFood = new Pair<float, float>(first, DaysUntilRotCalculator.ApproxDaysUntilRot(this.transferables, this.map.Tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, null, 0f, 3300));
                }
                return this.cachedDaysWorthOfFood;
            }
        }

        private Pair<ThingDef, float> ForagedFoodPerDay
        {
            get
            {
                if (this.foragedFoodPerDayDirty)
                {
                    this.foragedFoodPerDayDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedForagedFoodPerDay = ForagedFoodPerDayCalculator.ForagedFoodPerDay(this.transferables, this.Biome, Faction.OfPlayer, stringBuilder);
                    this.cachedForagedFoodPerDayExplanation = stringBuilder.ToString();
                }
                return this.cachedForagedFoodPerDay;
            }
        }

        private float Visibility
        {
            get
            {
                if (this.visibilityDirty)
                {
                    this.visibilityDirty = false;
                    StringBuilder stringBuilder = new StringBuilder();
                    this.cachedVisibility = CaravanVisibilityCalculator.Visibility(this.transferables, stringBuilder);
                    this.cachedVisibilityExplanation = stringBuilder.ToString();
                }
                return this.cachedVisibility;
            }
        }

        public Dialog_LoadShip(Map map, ShipBase ship)
        {
            this.map = map;
            this.ship = ship;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            this.CalculateAndRecacheTransferables();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, 35f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "LoadTransporters".Translate(new object[]
            {
                this.TransportersLabel
            }));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            CaravanUIUtility.DrawCaravanInfo(new CaravanUIUtility.CaravanInfo(this.MassUsage, this.MassCapacity, string.Empty, this.TilesPerDay, this.cachedTilesPerDayExplanation, this.DaysWorthOfFood, this.ForagedFoodPerDay, this.cachedForagedFoodPerDayExplanation, this.Visibility, this.cachedVisibilityExplanation, -1, -1, this.cachedCaravanMassCapacityExplanation), null, this.map.Tile, null, this.lastMassFlashTime, new Rect(12f, 35f, inRect.width - 24f, 40f), false, null, false);
            Dialog_LoadShip.tabsList.Clear();
            Dialog_LoadShip.tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate
            {
                this.tab = Dialog_LoadShip.Tab.Pawns;
            }, this.tab == Dialog_LoadShip.Tab.Pawns));
            Dialog_LoadShip.tabsList.Add(new TabRecord("ItemsTab".Translate(), delegate
            {
                this.tab = Dialog_LoadShip.Tab.Items;
            }, this.tab == Dialog_LoadShip.Tab.Items));
            inRect.yMin += 119f;
            Widgets.DrawMenuSection(inRect);
            TabDrawer.DrawTabs(inRect, Dialog_LoadShip.tabsList, 200f);
            inRect = inRect.ContractedBy(17f);
            GUI.BeginGroup(inRect);
            Rect rect2 = inRect.AtZero();
            this.DoBottomButtons(rect2);
            Rect inRect2 = rect2;
            inRect2.yMax -= 59f;
            bool flag = false;
            Dialog_LoadShip.Tab tab = this.tab;
            if (tab != Dialog_LoadShip.Tab.Pawns)
            {
                if (tab == Dialog_LoadShip.Tab.Items)
                {
                    this.itemsTransfer.OnGUI(inRect2, out flag);
                }
            }
            else
            {
                this.pawnsTransfer.OnGUI(inRect2, out flag);
            }
            if (flag)
            {
                this.CountToTransferChanged();
            }
            GUI.EndGroup();
        }

        public override bool CausesMessageBackground()
        {
            return true;
        }

        private void AddToTransferables(Thing t)
        {
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(t, this.transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (transferableOneWay == null)
            {
                transferableOneWay = new TransferableOneWay();
                this.transferables.Add(transferableOneWay);
            }
            transferableOneWay.things.Add(t);
        }

        private void DoBottomButtons(Rect rect)
        {
            Rect rect2 = new Rect(rect.width / 2f - this.BottomButtonSize.x / 2f, rect.height - 55f, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, false, true))
            {
                if (this.TryAccept())
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    this.Close(false);
                }
            }
            Rect rect3 = new Rect(rect2.x - 10f - this.BottomButtonSize.x, rect2.y, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect3, "ResetButton".Translate(), true, false, true))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                this.CalculateAndRecacheTransferables();
            }
            Rect rect4 = new Rect(rect2.xMax + 10f, rect2.y, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect4, "CancelButton".Translate(), true, false, true))
            {
                this.Close(true);
            }
            if (Prefs.DevMode)
            {
                float width = 200f;
                float num = this.BottomButtonSize.y / 2f;
                Rect rect5 = new Rect(0f, rect.height - 55f, width, num);
                if (Widgets.ButtonText(rect5, "Dev: Load instantly", true, false, true) && this.DebugTryLoadInstantly())
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    this.Close(false);
                }
                Rect rect6 = new Rect(0f, rect.height - 55f + num, width, num);
                if (Widgets.ButtonText(rect6, "Dev: Select everything", true, false, true))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    this.SetToLoadEverything();
                }
            }
        }

        private void CalculateAndRecacheTransferables()
        {
            this.transferables = new List<TransferableOneWay>();
            this.AddPawnsToTransferables();
            this.AddItemsToTransferables();
            IEnumerable<TransferableOneWay> enumerable = null;
            string text = null;
            string destinationLabel = null;
            string text2 = "FormCaravanColonyThingCountTip".Translate();
            bool flag = true;
            IgnorePawnsInventoryMode ignorePawnInventoryMass = IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload;
            bool flag2 = true;
            Func<float> availableMassGetter = () => this.MassCapacity - this.MassUsage;
            int tile = this.map.Tile;
            this.pawnsTransfer = new TransferableOneWayWidget(enumerable, text, destinationLabel, text2, flag, ignorePawnInventoryMass, flag2, availableMassGetter, 0f, false, tile, true, true, true, false, true, false, false);
            CaravanUIUtility.AddPawnsSections(this.pawnsTransfer, this.transferables);
            enumerable = from x in this.transferables
                         where x.ThingDef.category != ThingCategory.Pawn
                         select x;
            text2 = null;
            destinationLabel = null;
            text = "FormCaravanColonyThingCountTip".Translate();
            flag2 = true;
            ignorePawnInventoryMass = IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload;
            flag = true;
            availableMassGetter = (() => this.MassCapacity - this.MassUsage);
            tile = this.map.Tile;
            this.itemsTransfer = new TransferableOneWayWidget(enumerable, text2, destinationLabel, text, flag2, ignorePawnInventoryMass, flag, availableMassGetter, 0f, false, tile, true, false, false, true, false, true, false);
            this.CountToTransferChanged();
        }

        private bool DebugTryLoadInstantly()
        {
            int i;
            for (i = 0; i < this.transferables.Count; i++)
            {
                TransferableUtility.Transfer(this.transferables[i].things, this.transferables[i].CountToTransfer, delegate (Thing splitPiece, IThingHolder originalThing)
                {
                    this.ship.GetDirectlyHeldThings().TryAdd(splitPiece, true);
                });
            }
            return true;
        }

        private bool TryAccept()
        {
            List<Pawn> pawnsFromTransferables = TransferableUtility.GetPawnsFromTransferables(this.transferables);
            if (!this.CheckForErrors(pawnsFromTransferables))
            {
                return false;
            }
            this.AssignTransferablesToShip();
            IEnumerable<Pawn> enumerable = from x in pawnsFromTransferables
                                           where x.IsColonist && !x.Downed
                                           select x;
            if (enumerable.Any<Pawn>())
            {
                foreach (Pawn current in enumerable)
                {
                    Lord lord = current.GetLord();
                    if (lord != null)
                    {
                        lord.Notify_PawnLost(current, PawnLostCondition.ForcedToJoinOtherLord, null);
                    }
                }
                LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_LoadShipCargo(this.ship), this.map, enumerable);
                foreach (Pawn current2 in enumerable)
                {
                    if (current2.Spawned)
                    {
                        current2.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                    }
                }
            }
            Messages.Message("MessageShipCargoLoadStarted".Translate(new object[] { ship.ShipNick }), this.ship, MessageTypeDefOf.TaskCompletion, false);
            return true;
        }

        private bool AssignTransferablesToShip()
        {
            this.ship.compShip.leftToLoad = new List<TransferableOneWay>();
            this.ship.compShip.leftToLoad.Clear();

            //     this.RemoveExistingTransferables();
            for (int i = 0; i < this.transferables.Count; i++)
            {
                Dialog_LoadShip.RemoveExistingTransferable(this.transferables[i], null, this.ship);
                if (this.transferables[i].CountToTransfer > 0)
                {
                    this.ship.compShip.AddToTheToLoadList(this.transferables[i], this.transferables[i].CountToTransfer);
                    //                   TransferableUIUtility.ClearEditBuffer(this.transferables[i]);
                }
            }
            return true;
        }

        public static void RemoveExistingTransferable(TransferableOneWay transferable, Map map = null, ShipBase ship = null)
        {
            List<Thing> thingsInCargoToRemov = new List<Thing>();
            List<ShipBase> tmpShips = new List<ShipBase>();
            if (ship != null)
            {
                tmpShips.Add(ship);
            }
            else if (map != null)
            {
                tmpShips = DropShipUtility.ShipsOnMap(map);
            }
            else
            {
                Log.Error("Tried removing transferables with neither ship nor map specified");
            }
        }

            private bool CheckForErrors(List<Pawn> pawns)
        {
            if (!this.transferables.Any((TransferableOneWay x) => x.CountToTransfer != 0))
            {
                Messages.Message("CantSendEmptyTransportPods".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (this.numHaulers <= 0 && pawns.Count <= 0)
            {
                Messages.Message("CantAssignZeroHaulers".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (this.MassUsage > this.MassCapacity)
            {
                this.FlashMass();
                Messages.Message("TooBigShipMassUsage".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            if (this.CurrentPassengerCount > this.MaxPassengerSeats)
            {
                Messages.Message("ShipSeatsFull".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }
            Pawn pawn = pawns.Find((Pawn x) => !x.MapHeld.reachability.CanReach(x.PositionHeld, this.ship, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)));
            if (pawn != null)
            {
                Messages.Message("PawnCantReachTransporters".Translate(new object[]
                {
                    pawn.LabelShort
                }).CapitalizeFirst(), MessageTypeDefOf.RejectInput);
                return false;
            }
            Map map = this.ship.Map;
            for (int i = 0; i < this.transferables.Count; i++)
            {
                if (this.transferables[i].ThingDef.category == ThingCategory.Item)
                {
                    int CountToTransfer = this.transferables[i].CountToTransfer;
                    int num = 0;
                    if (CountToTransfer > 0)
                    {
                        for (int j = 0; j < this.transferables[i].things.Count; j++)
                        {
                            Thing thing = this.transferables[i].things[j];
                            if (map.reachability.CanReach(thing.Position, this.ship, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)))
                            {
                                num += thing.stackCount;
                                if (num >= CountToTransfer)
                                {
                                    break;
                                }
                            }
                        }
                        if (num < CountToTransfer)
                        {
                            if (CountToTransfer == 1)
                            {
                                Messages.Message("TransporterItemIsUnreachableSingle".Translate(new object[]
                                {
                                    this.transferables[i].ThingDef.label
                                }), MessageTypeDefOf.RejectInput);
                            }
                            else
                            {
                                Messages.Message("TransporterItemIsUnreachableMulti".Translate(new object[]
                                {
                                    CountToTransfer,
                                    this.transferables[i].ThingDef.label
                                }), MessageTypeDefOf.RejectInput);
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void AddPawnsToTransferables()
        {
            List<Pawn> list = CaravanFormingUtility.AllSendablePawns(this.map, false, false, false);
            for (int i = 0; i < list.Count; i++)
            {
                this.AddToTransferables(list[i]);
            }
        }

        private void AddItemsToTransferables()
        {
            List<Thing> list = CaravanFormingUtility.AllReachableColonyItems(this.map, false, false, false);
            for (int i = 0; i < list.Count; i++)
            {
                this.AddToTransferables(list[i]);
            }
        }

        private void FlashMass()
        {
            this.lastMassFlashTime = Time.time;
        }

        private void SetToLoadEverything()
        {
            for (int i = 0; i < this.transferables.Count; i++)
            {
                this.transferables[i].AdjustTo(this.transferables[i].GetMaximumToTransfer());
            }
            this.CountToTransferChanged();
        }

        private void CountToTransferChanged()
        {
            this.massUsageDirty = true;
            this.caravanMassUsageDirty = true;
            this.caravanMassCapacityDirty = true;
            this.tilesPerDayDirty = true;
            this.daysWorthOfFoodDirty = true;
            this.foragedFoodPerDayDirty = true;
            this.visibilityDirty = true;
        }
    }

}
