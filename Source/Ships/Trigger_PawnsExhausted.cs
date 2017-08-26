using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse.AI;
using Verse.AI.Group;

namespace OHUShips
{
    public class Trigger_PawnsExhausted : Trigger
    {
        private float extraRestThreshOffset;

        public Trigger_PawnsExhausted(float extraRestThreshOffset = 0f)
        {
            this.extraRestThreshOffset = extraRestThreshOffset;
        }

        public override bool ActivateOn(Lord lord, TriggerSignal signal)
        {
            if (signal.type == TriggerSignalType.Tick)
            {
                for (int i = 0; i < lord.ownedPawns.Count; i++)
                {
                    if (GenAI.EnemyIsNear(lord.ownedPawns[i], 10f))
                    {
                        return false;
                    }
                    Need_Rest rest = lord.ownedPawns[i].needs.rest;
                    if (rest != null)
                    {
                        if (rest.CurLevelPercentage < 0.14f + this.extraRestThreshOffset && !lord.ownedPawns[i].Awake())
                        {
                            return true;
                        }
                    }
                    Need_Food food = lord.ownedPawns[i].needs.food;
                    if (food != null)
                    {
                        if (food.CurCategory == HungerCategory.Starving)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            return false;
        }
    }
}
