using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse.AI.Group;
using Verse;

namespace OHUShips
{
    public class LordJob_LoadShipCargo : LordJob
    {
        public ShipBase ship;

        private int TimeOutTick = 7500;

        public LordJob_LoadShipCargo()
        {
        }

        public LordJob_LoadShipCargo(ShipBase ship)
        {
            this.ship = ship;
        }
        
        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            LordToil_LoadShipCargo loadToil = new LordToil_LoadShipCargo(ship);
            stateGraph.AddToil(loadToil);

            LordToil_End lordToil_End = new LordToil_End();
            stateGraph.AddToil(lordToil_End);

            Transition transition = new Transition(loadToil, lordToil_End);
            transition.AddTrigger(new Trigger_PawnLost());
            
            transition.AddPreAction(new TransitionAction_Message("MessageFailedToLoadTransportersBecauseColonistLost".Translate(), MessageSound.Negative));
            transition.AddPreAction(new TransitionAction_Custom(new Action(this.CancelLoadingProcess)));
            stateGraph.AddTransition(transition);

            Transition endTransition = new Transition(loadToil, lordToil_End);
            endTransition.AddTrigger(new Trigger_TicksPassed(TimeOutTick));
            endTransition.AddTrigger(new Trigger_PawnsExhausted());
            endTransition.AddPreAction(new TransitionAction_Custom(new Action(delegate {
            Log.Message("Ending");
            this.CancelLoadingProcess(); })));
            stateGraph.AddTransition(endTransition);
            return stateGraph;
        }

        private void CancelLoadingProcess()
        {
            this.ship.compShip.CancelLoadCargo(this.Map);
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<ShipBase>(ref this.ship, "ship");
        }
    }
}
