using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections;
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
    public class Dialog_LoadShipCargo : Window
    {
        private enum Tab
        {
            Pawns,
            Items
        }

        private const float TitleRectHeight = 40f;

        private const float BottomAreaHeight = 55f;

        private int numOfHaulers;
        private string numOfHaulersString;

        private Map map;

        private ShipBase ship;

        private List<TransferableOneWay> transferables;

        private TransferableOneWayWidget pawnsTransfer;

        private TransferableOneWayWidget itemsTransfer;

        private Dialog_LoadShipCargo.Tab tab;

        private float lastMassFlashTime = -9999f;

        private bool massUsageDirty = true;

        private float cachedMassUsage;

        private bool daysWorthOfFoodDirty = true;

        private Pair<float, float> cachedDaysWorthOfFood;

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

        private float PassengerCapacity
        {
            get
            {
                return this.ship.compShip.sProps.maxPassengers;
            }
        }

        private string TransportersLabelFull
        {
            get
            {
                return this.ship.LabelCap + " : " + this.ship.ShipNick;
            }
        }

        private string TransportersLabelShort
        {
            get
            {
                return this.ship.ShipNick.CapitalizeFirst();
            }
        }

        private float MassUsage
        {
            get
            {
                if (this.massUsageDirty)
                {
                    this.massUsageDirty = false;
                    this.cachedMassUsage = CollectionsMassCalculator.MassUsageTransferables(this.transferables, IgnorePawnsInventoryMode.DontIgnore, true, false);
                    this.cachedMassUsage += MassAlreadyStored();
                }
                return this.cachedMassUsage;
            }
        }

        public float MassAlreadyStored()
        {
            float num = 0f;
            for (int i=0; i < this.ship.GetDirectlyHeldThings().Count; i++)
            {
                num += this.ship.GetDirectlyHeldThings()[i].stackCount * this.ship.GetDirectlyHeldThings()[i].GetStatValue(StatDefOf.Mass);
            }
            return num;
        }

        private Pair<float, float> DaysWorthOfFood
        {
            get
            {
                if (this.daysWorthOfFoodDirty)
                {
                    this.daysWorthOfFoodDirty = false;
                    float first = DropShipUtility.ApproxDaysWorthOfFood_Ship(ship, this.transferables, this.EnvironmentAllowsEatingVirtualPlantsNow);
                    this.cachedDaysWorthOfFood = new Pair<float, float>(first, DaysUntilRotCalculator.ApproxDaysUntilRot(this.transferables, this.map.Tile, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload));
                }
                return this.cachedDaysWorthOfFood;
            }
        }
       
        public Dialog_LoadShipCargo(Map map, ShipBase ship)
        {
            this.map = map;
            this.ship = ship;
            this.closeOnEscapeKey = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            OHUShipsModSettings.CargoLoadingActive = true;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            this.CalculateAndRecacheTransferables();
        }

        private bool EnvironmentAllowsEatingVirtualPlantsNow
        {
            get
            {
                return VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(this.map.Tile);
            }
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

            for (int j = 0; j < transferable.things.Count; j++)
            {
                for (int k = 0; k < tmpShips.Count; k++)
                {
                    Thing thing = tmpShips[k].GetDirectlyHeldThings().FirstOrDefault(x => x == (transferable.things[j]));
                    if (thing != null)
                    {
                        Log.Message("FoundCargo");
                        thingsInCargoToRemov.Add(transferable.things[j]);
                        //                  transferable.CountToTransfer -= transferable.things[j].stackCount;
                    }
                }
            }
            transferable.things.RemoveAll(x => thingsInCargoToRemov.Contains(x));
        }

        private int PawnsToTransfer
        {
            get
            {
                return this.transferables.Where(x => x.AnyThing is Pawn && x.CountToTransfer > 0).Count();
            }
        }

        private string PassengerUse
        {
            get
            {
                return this.PawnsToTransfer + "/" + this.PassengerCapacity + " " +"ShipPassengers".Translate();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = new Rect(0f, 0f, inRect.width, 40f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "LoadTransporters".Translate(new object[]
            {
                this.TransportersLabelFull
            }));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Dialog_LoadShipCargo.tabsList.Clear();
            Dialog_LoadShipCargo.tabsList.Add(new TabRecord("PawnsTab".Translate(), delegate
            {
                this.tab = Dialog_LoadShipCargo.Tab.Pawns;
            }, this.tab == Dialog_LoadShipCargo.Tab.Pawns));
            Dialog_LoadShipCargo.tabsList.Add(new TabRecord("ItemsTab".Translate(), delegate
            {
                this.tab = Dialog_LoadShipCargo.Tab.Items;
            }, this.tab == Dialog_LoadShipCargo.Tab.Items));
            inRect.yMin += 72f;
            Widgets.DrawMenuSection(inRect, true);
            TabDrawer.DrawTabs(inRect, Dialog_LoadShipCargo.tabsList);
            inRect = inRect.ContractedBy(17f);
            GUI.BeginGroup(inRect);
            Rect rect2 = inRect.AtZero();
            Rect rect3 = rect2;
            rect3.xMin += rect2.width - this.pawnsTransfer.TotalNumbersColumnsWidths;
            rect3.y += 32f;
            TransferableUIUtility.DrawMassInfo(rect3, this.MassUsage, this.MassCapacity, "TransportersMassUsageTooltip".Translate(), this.lastMassFlashTime, true);
            CaravanUIUtility.DrawDaysWorthOfFoodInfo(new Rect(rect3.x, rect3.y + 22f, rect3.width, rect3.height), this.DaysWorthOfFood.First, this.DaysWorthOfFood.Second, this.EnvironmentAllowsEatingVirtualPlantsNow, true, 3.40282347E+38f);
            this.DrawPassengerCapacity(rect3);

            this.DoBottomButtons(rect2);
            Rect inRect2 = rect2;
            inRect2.yMax += 59f;
            bool flag = false;
            Dialog_LoadShipCargo.Tab tab = this.tab;
            if (tab != Dialog_LoadShipCargo.Tab.Pawns)
            {
                if (tab == Dialog_LoadShipCargo.Tab.Items)
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

        private void AddToTransferables(Thing t, int countAlreadyIn = 0)
        {
            TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(t, this.transferables);
            if (transferableOneWay == null)
            {
                transferableOneWay = new TransferableOneWay();
                this.transferables.Add(transferableOneWay);
            }

            // Dialog_LoadShipCargo.RemoveExistingTransferable(transferableOneWay, null, this.ship);

            transferableOneWay.things.Add(t);
            transferableOneWay.AdjustBy(-countAlreadyIn);

        }

        private void DoBottomButtons(Rect rect)
        {
            Rect rect0 = new Rect(0f, rect.height - 55f, 300f, 30f);
            
            Widgets.TextFieldNumericLabeled(rect0, "NumberOfHaulers".Translate(), ref this.numOfHaulers, ref this.numOfHaulersString, 0, map.mapPawns.FreeColonistsSpawned.ToList().FindAll(x => x.GetLord() == null).Count);

            Rect rect2 = new Rect(rect.width / 2f - this.BottomButtonSize.x / 2f, rect.height - 55f, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect2, "AcceptButton".Translate(), true, false, true) && this.TryAccept())
            {
                SoundDefOf.TickHigh.PlayOneShotOnCamera();
                this.Close(false);
            }
            Rect rect3 = new Rect(rect2.x - 10f - this.BottomButtonSize.x, rect2.y, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect3, "ResetButton".Translate(), true, false, true))
            {
                SoundDefOf.TickLow.PlayOneShotOnCamera();
                this.CalculateAndRecacheTransferables();
            }
            Rect rect4 = new Rect(rect2.xMax + 10f, rect2.y, this.BottomButtonSize.x, this.BottomButtonSize.y);
            if (Widgets.ButtonText(rect4, "CancelButton".Translate(), true, false, true))
            {
                this.Close(true);
            }
            if (Prefs.DevMode)
            {
                float num = 200f;
                float num2 = this.BottomButtonSize.y / 2f;
                Rect rect5 = new Rect(rect.width - num, rect.height - 55f, num, num2);
                if (Widgets.ButtonText(rect5, "Dev: Load instantly", true, false, true) && this.DebugTryLoadInstantly())
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera();
                    this.Close(false);
                }
                Rect rect6 = new Rect(rect.width - num, rect.height - 55f + num2, num, num2);
                if (Widgets.ButtonText(rect6, "Dev: Select everything", true, false, true))
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera();
                    this.SetToLoadEverything();
                }
            }
        }

        private void DrawPassengerCapacity(Rect rect3)
        {
            GUI.color = this.PawnsToTransfer > this.PassengerCapacity ? Color.red : Color.gray;
            Vector3 vector = Text.CalcSize(this.PassengerUse);
            Rect rect2 = new Rect(rect3.xMax - vector.x, rect3.y + 44f, vector.x, vector.y);
            Widgets.Label(rect2, this.PassengerUse);
            GUI.color = Color.white;
        }

        private void CalculateAndRecacheTransferables()
        {
            this.transferables = new List<TransferableOneWay>();
            this.AddPawnsToTransferables();
            this.AddItemsToTransferables();
        //    this.RemoveExistingTransferables();
            this.pawnsTransfer = new TransferableOneWayWidget(null, Faction.OfPlayer.Name, this.TransportersLabelShort, "FormCaravanColonyThingCountTip".Translate(), true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true, () => this.MassCapacity - this.MassUsage, 24f, false, true);
            CaravanUIUtility.AddPawnsSections(this.pawnsTransfer, this.transferables);

            this.itemsTransfer = new TransferableOneWayWidget(from x in this.transferables
                                                              where x.ThingDef.category != ThingCategory.Pawn
                                                              select x, Faction.OfPlayer.Name, this.TransportersLabelShort, "FormCaravanColonyThingCountTip".Translate(), true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true, () => this.MassCapacity - this.MassUsage, 24f, false, true);
            this.CountToTransferChanged();            
        }

        private bool DebugTryLoadInstantly()
        {
            for (int i = 0; i < this.transferables.Count; i++)
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
            if (!this.AssignTransferablesToShip())
            {
                return false;
            }
            IEnumerable<Pawn> enumerable = from x in pawnsFromTransferables
                                           where x.IsColonist && !x.Downed
                                           select x;
            List<Pawn> list = enumerable.ToList();
            while(list.Count(x => x.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) < this.numOfHaulers)
            {
                Pawn pawn = map.mapPawns.FreeColonistsSpawned.RandomElement();

                if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && !list.Contains(pawn) && pawn.GetLord() == null)
                {
                    list.Add(pawn);
                }
            }

            if (list.Any<Pawn>())
            {
                foreach (Pawn current in enumerable)
                {
                    if (current.Spawned)
                    {
                        current.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                    }
                }
               Lord newLord = LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_LoadShipCargo(this.ship), this.map, list);
            }
            ship.compShip.cargoLoadingActive = true;
            Messages.Message("MessageShipCargoLoadStarted".Translate( new object[] { ship.ShipNick }), ship, MessageSound.Benefit);
            return true;
        }

        private bool AssignTransferablesToShip()
        {
            this.ship.compShip.leftToLoad = new List<TransferableOneWay>();
            this.ship.compShip.leftToLoad.Clear();

       //     this.RemoveExistingTransferables();
            for (int i = 0; i < this.transferables.Count; i++)
            {
                Dialog_LoadShipCargo.RemoveExistingTransferable(this.transferables[i], null, this.ship);
                if (this.transferables[i].CountToTransfer > 0)
                {
                    this.ship.compShip.AddToTheToLoadList(this.transferables[i], this.transferables[i].CountToTransfer);
 //                   TransferableUIUtility.ClearEditBuffer(this.transferables[i]);
                }             
            }
            return true;
        }        

        private bool CheckForErrors(List<Pawn> pawns)
        {
            if (!this.transferables.Any((TransferableOneWay x) => x.CountToTransfer != 0))
            {
                Messages.Message("CantSendEmptyTransportPods".Translate(), MessageSound.RejectInput);
                return false;
            }
            if (this.numOfHaulers <= 0 && pawns.Count <= 0)
            {
                Messages.Message("CantAssignZeroHaulers".Translate(), MessageSound.RejectInput);
                return false;
            }
            if (this.MassUsage > this.MassCapacity)
            {
                this.FlashMass();
                Messages.Message("TooBigShipMassUsage".Translate(), MessageSound.RejectInput);
                return false;
            }
            if (this.PawnsToTransfer > this.PassengerCapacity)
            {
                Messages.Message("ShipSeatsFull".Translate(), MessageSound.RejectInput);
                return false;
            }
            Pawn pawn = pawns.Find((Pawn x) => !x.MapHeld.reachability.CanReach(x.PositionHeld, this.ship, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false)));
            if (pawn != null)
            {
                Messages.Message("PawnCantReachTransporters".Translate(new object[]
                {
                    pawn.LabelShort
                }).CapitalizeFirst(), MessageSound.RejectInput);
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
                                }), MessageSound.RejectInput);
                            }
                            else
                            {
                                Messages.Message("TransporterItemIsUnreachableMulti".Translate(new object[]
                                {
                                    CountToTransfer,
                                    this.transferables[i].ThingDef.label
                                }), MessageSound.RejectInput);
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
            List<Pawn> list = CaravanFormingUtility.AllSendablePawns(this.map, false, false);
            for (int i = 0; i < list.Count; i++)
            {
                this.AddToTransferables(list[i]);
            }
        }

        private bool isPlayerBase
        {
            get
            {
                FactionBase mapParent = Find.WorldObjects.FactionBaseAt(this.ship.Tile);
                if (mapParent != null)
                {
                    if (mapParent.Faction != Faction.OfPlayer)
                    return true;
                }
                return false;
            }
        }

        private void AddItemsToTransferables()
        {
           // List<Thing> list = CaravanFormingUtility.AllReachableColonyItems(this.map, false, false);

            List<Thing> list = CaravanFormingUtility.AllReachableColonyItems(this.map, false, isPlayerBase);
            for (int i = 0; i < list.Count; i++)
            {
                int alreadyIn = 0;
                Thing thingAlreadyIn = this.ship.GetDirectlyHeldThings().FirstOrDefault(x => x == list[i]);
                if (thingAlreadyIn != null)
                {
                    alreadyIn = thingAlreadyIn.stackCount;
                }
                this.AddToTransferables(list[i], alreadyIn);
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
                TransferableUtility.Transfer(this.transferables[i].things, this.transferables[i].CountToTransfer, delegate (Thing splitPiece, IThingHolder originalThing)
                {
                    this.ship.GetDirectlyHeldThings().TryAdd(splitPiece, true);
                });
            }
            this.CountToTransferChanged();
        }

        private void CountToTransferChanged()
        {
            this.massUsageDirty = true;
            this.daysWorthOfFoodDirty = true;
        }

        public override void Close(bool doCloseSound = true)
        {
            OHUShipsModSettings.CargoLoadingActive = false;
            base.Close(doCloseSound);
        }
    }
}
