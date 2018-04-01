using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace OHUShips
{
    public class ShipTracker : WorldObject
    {
        public override bool SelectableNow
        {
            get
            {
                return false;
            }
        }

        public override void Draw()
        {
        }

        private int nextFleetID = 0;

        private int nextWeaponSlotID = 0;

        public Dictionary<int, string> PlayerFleetManager = new Dictionary<int, string>();

        public Dictionary<string, List<ShipBase>> shipsInFlight = new Dictionary<string, List<ShipBase>>();

        public List<ShipBase> AllWorldShips = new List<ShipBase>();

        public List<LandedShip> AllLandedShips
        {
            get
            {
                List<LandedShip> tmp = new List<LandedShip>();
                for (int i = 0; i < Find.WorldObjects.AllWorldObjects.Count; i++)
                {
                    LandedShip ship = Find.WorldObjects.AllWorldObjects[i] as LandedShip;
                    if (ship != null)
                    {
                        tmp.Add(ship);
                    }
                }
                return tmp;
            }
        }
        
        public List<TravelingShips> AllTravelingShips
        {
            get
            {
                List<TravelingShips> tmp = new List<TravelingShips>();
                for (int i=0; i < Find.WorldObjects.AllWorldObjects.Count; i++)
                {
                    TravelingShips ship = Find.WorldObjects.AllWorldObjects[i] as TravelingShips;
                    if (ship != null)
                    {
                        tmp.Add(ship);
                    }
                }
                return tmp;
            }
        }

        public void RemoveShip(ShipBase ship)
        {
            this.AllWorldShips.Remove(ship);
            this.AllWorldShips.RemoveAll(x => x == null);
        }

        public List<ShipBase> PlayerShips
        {
            get
            {
                return this.AllWorldShips.FindAll(x => x.Faction == Faction.OfPlayer);
            }
        }
        public void AddNewFleetEntry()
        {
            this.PlayerFleetManager.Add(this.GetNextFleetId(), "TabFleetManagement".Translate() + " " + this.nextFleetID);
        }
        public void AddNewFleetEntry(string newName)
        {
            this.PlayerFleetManager.Add(this.GetNextFleetId(), newName);
        }
        public void DeleteFleetEntry(int ID)
        {
            this.PlayerFleetManager.Remove(ID);
        }
        
        public int GetNextFleetId()
        {
            return this.GetNextID(ref this.nextFleetID);
        }

        private int GetNextID(ref int nextID)
        {
            if (Scribe.mode == LoadSaveMode.Saving || Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Log.Warning("Getting next unique ID during saving or loading. This may cause bugs.");
            }
            int result = nextID;
            nextID++;
            if (nextID == 2147483647)
            {
                Log.Warning("Next ID is at max value. Resetting to 0. This may cause bugs.");
                nextID = 0;
            }
            return result;
        }

        public int GetNextWeaponSlotID()
        {
            return this.GetNextID(ref this.nextWeaponSlotID);
        }

        public List<ShipBase> ShipsInFleet(int ID)
        {
            return this.AllWorldShips.FindAll(x => x.fleetID == ID);
        }

        public bool PawnIsTravelingInShip(Pawn pawn)
        {
            for(int i = 0; i < this.AllTravelingShips.Count; i++)
            {
                TravelingShips cur = this.AllTravelingShips[i];
                if (cur.ContainsPawn(pawn))
                {
                    return true;
                }
            }

            return false;
        }
                
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.nextFleetID, "nextFleetID");
            Scribe_Values.Look<int>(ref this.nextWeaponSlotID, "nextWeaponSlotID");
            Scribe_Collections.Look<int, string>(ref this.PlayerFleetManager, "PlayerFleetManager", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look<ShipBase>(ref this.AllWorldShips, "AllWorldShips", LookMode.Reference, new object[0]);            
        }
    }
}
