using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse.AI.Group;

namespace OHUShips
{
    public class LordJob_StealForShip : LordJob_Steal 
    {
        public List<ShipBase> ships = new List<ShipBase>();

        public LordJob_StealForShip(ShipBase ship)
        {

        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = base.CreateGraph();
            List<Transition> leaveTransitions = graph.transitions.FindAll(x => x.target.GetType() == typeof(LordToil_ExitMapAndEscortCarriers));
            for (int i = 0; i < leaveTransitions.Count; i++)
            {
                LordToil_LeaveInShip lordToil = new LordToil_LeaveInShip();
                leaveTransitions[i].target = lordToil;

                graph.AddToil(lordToil);
                Transition transition = new Transition(leaveTransitions[i].target, new LordToil_ExitMapAndEscortCarriers());
                transition.AddTrigger(new Trigger_Custom((TriggerSignal x) => this.ships.All(z => z.Destroyed) || !this.ships.Any(y => y.Map == this.Map && y.Spawned)));
                graph.transitions.Add(transition);
            }

            return graph;

        }

    }
}
