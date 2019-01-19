using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OHUShips
{
    public class WorldShipPather : IExposable
    {
        private const int BASE_TICKS_PER_MOVE = 100;
        private List<int> pathNodes = new List<int>();

        internal enum WorldShipPatherStatus
        {
            Standby,
            Travelling
        }

        public WorldShipPather(WorldShip worldShip)
        {
            this._worldShip = worldShip;
        }

        private WorldShip _worldShip;
        int _destinationTile;
        IntVec3 _destinationCell;
        int _startTile;
        private WorldShipPatherStatus status;
        private int currentTileTraversalTicksLeft;
        private bool moving;
        private ShipArrivalAction _arrivalAction;
        private PawnsArrivalModeDef _mapArrivalMode;
        private WorldPath worldPath;

        public void PatherTick()
        {
            if (this.status == WorldShipPatherStatus.Travelling && this.Moving)
            {
                this.TraverseTiles();
            }
        }

        public WorldPath WorldPath
        {
            get
            {
                return this.worldPath;
            }
        }

        public bool IsSpawned => Find.World.worldObjects.Contains(this._worldShip);
        public bool Moving
        {
            get
            {
                return IsSpawned && this.moving;
            }
        }
        private int MinTicksPerTile
        {
            get
            {
                return (int)(BASE_TICKS_PER_MOVE / this.SlowestShipSpeedFactor);
            }
        }

        public float CurrentAngleOffset
        {
            get
            {
                return Find.WorldGrid.GetHeadingFromTo(this._startTile, this._destinationTile);
            }
        }

        private float TravelledPercentage
        {
            get
            {
                return ((float)(CurrentPathLength - this.worldPath.NodesLeftCount) + (CurrentTileTravelPercentage)) / (float)this.CurrentPathLength;
            }
        }

        private float CurrentTileTravelPercentage
        {
            get
            {
                return 1f - ((float)this.currentTileTraversalTicksLeft / this.MinTicksPerTile);
            }
        }

        private int drawTileStart;
        private int drawTileEnd;
        private int currentDrawTicksLeft;

        public Vector3 GetDrawPos()
        {
            Vector3 start = Find.WorldGrid.GetTileCenter(this._startTile);
            Vector3 end = Find.WorldGrid.GetTileCenter(this._destinationTile);
            return Vector3.Slerp(start, end, this.TravelledPercentage);

            //if (this.WorldPath.NodesLeftCount > 2 && this._worldShip.Tile == this.drawTileEnd)
            //{
            //    this.drawTileStart = this._worldShip.Tile;
            //    drawTileEnd = this.WorldPath.Peek(2);
            //    this.currentDrawTicksLeft = 3 * this.MinTicksPerTile;
            //}
            //Vector3 start = Find.WorldGrid.GetTileCenter(drawTileStart);
            //Vector3 end = Find.WorldGrid.GetTileCenter(drawTileEnd);
            //Vector3 vector = Vector3.Slerp(start, end, (float)this.currentDrawTicksLeft / (this.MinTicksPerTile * 3));
            //return vector;
        }
        
        private float SlowestShipSpeedFactor
        {
            get
            {
                return this._worldShip.WorldShipData.Min(x => x.Ship.compShip.sProps.WorldMapTravelSpeedFactor);
            }
        }

        private int CurrentPathLength => this.worldPath.NodesReversed.Count;
        
        private Vector3 Start
        {
            get
            {
                return Find.WorldGrid.GetTileCenter(this._startTile);
            }
        }

        private Vector3 End
        {
            get
            {
                return Find.WorldGrid.GetTileCenter(this._destinationTile);
            }
        }

        public void SetDestination(int destTile, IntVec3 destinationCell, ShipArrivalAction arrivalAction, PawnsArrivalModeDef pawnsArrivalMode)
        {
            if (this.worldPath != null)
            {
                this.worldPath.Dispose();
            }
            this._destinationTile = destTile;
            this._destinationCell = destinationCell;
            this._startTile = this._worldShip.Tile;
            this.drawTileStart = this._startTile;
            this.worldPath = DropShipUtility.CurrentShipTracker.WorldShipPathFinder.FindPath(_startTile, _destinationTile, this._worldShip, (int)this.MinTicksPerTile);
            this.status = WorldShipPatherStatus.Travelling;            
            this.moving = true;
            this._arrivalAction = arrivalAction;
            this._mapArrivalMode = pawnsArrivalMode;
            this.currentTileTraversalTicksLeft = this.MinTicksPerTile;
            this._worldShip.IsTargeting = false;

        }

        private float tileTraversalPercentage;

        public void TraverseTiles()
        {
            this.currentTileTraversalTicksLeft -= 1;
            if (this.currentTileTraversalTicksLeft <= 0)
            {
                this._worldShip.Tile = this.worldPath.ConsumeNextNode(); //this.Path[currentNodeIndex];
                this.currentTileTraversalTicksLeft = this.MinTicksPerTile;
                //Log.Message("Left:" + currentTileTraversalTicksLeft.ToString());
            }
            if (this._worldShip.Tile == this._destinationTile && this.currentTileTraversalTicksLeft == 1)
            {
                this.Arrive();
            }
        }
        
        private void Arrive()
        {
            this._worldShip.Arrive(this._destinationCell, _arrivalAction, _mapArrivalMode);
            this.status = WorldShipPatherStatus.Standby;
            this.moving = false;
            this.worldPath.Dispose();
        }

        public void Halt()
        {
            this.status = WorldShipPatherStatus.Standby;
            this.moving = false;
            this.worldPath.Dispose();
        }

        public void ToggleCircling()
        {
            this.moving = !this.moving;
        }

        public void ExposeData()
        {
            Scribe_References.Look<WorldShip>(ref this._worldShip, "worldShip");
            Scribe_Values.Look<ShipArrivalAction>(ref this._arrivalAction, "ArrivalAction");
            Scribe_Values.Look<float>(ref this.tileTraversalPercentage, "tileTraversalPercentage");
            Scribe_Values.Look<WorldShipPatherStatus>(ref this.status, "status", WorldShipPatherStatus.Standby);
            Scribe_Defs.Look<PawnsArrivalModeDef>(ref this._mapArrivalMode, "MapArrivalMode");
            Scribe_Values.Look<int>(ref this._destinationTile, "destinationTile");
            Scribe_Values.Look<IntVec3>(ref this._destinationCell, "destinationCell");
            Scribe_Values.Look<bool>(ref this.moving, "moving");
            Scribe_Values.Look<int>(ref this._startTile, "startTile");
            Scribe_Values.Look<int>(ref this.currentTileTraversalTicksLeft, "currentTileTicksLeft", BASE_TICKS_PER_MOVE);
            //Scribe_Values.Look<WorldPath>(ref this.worldPath, "worldPath");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && this.moving)
            {
                this.SetDestination(this._destinationTile, this._destinationCell, this._arrivalAction, this._mapArrivalMode);
            }
        }
              
    }
}