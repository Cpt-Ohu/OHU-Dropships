using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse.AI.Group;

namespace OHUShips
{
    public class LordJob_AerialAssault : LordJob_AssaultColony
    {
        private List<ShipBase> ships;

        public  LordJob_AerialAssault(List<ShipBase> ships,  Faction assaulterFaction, bool canKidnap = true, bool canTimeoutOrFlee = true, bool sappers = false, bool useAvoidGridSmart = false, bool canSteal = true) : base(assaulterFaction, canKidnap, canTimeoutOrFlee, sappers, useAvoidGridSmart, canSteal)
        {
            this.ships = ships;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = base.CreateGraph();
            List<Transition> leaveTransitions = graph.transitions.FindAll(x => x.target.GetType() == typeof(LordToil_ExitMapBest));
            for (int i=0; i < leaveTransitions.Count; i++)
            {
                leaveTransitions[i].target = new LordToil_LeaveInShip();

                Transition transition = new Transition(leaveTransitions[i].target, new LordToil_ExitMapBest());
                transition.AddTrigger(new Trigger_Custom((TriggerSignal x) => !this.ships.Any(y => y.Map == this.Map)));
                graph.transitions.Add(transition);
            }

            return graph;

        }

    }
}
