﻿using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace OHUShips
{
    /// <summary>
    /// Emulates the pathfinding mechanism of the Caravan system, but ignores passability.
    /// </summary>
    public class WorldShipPathFinder
    {
        private struct CostNode
        {
            public int tile;

            public int cost;

            public CostNode(int tile, int cost)
            {
                this.tile = tile;
                this.cost = cost;
            }
        }

        private struct PathFinderNodeFast
        {
            public int knownCost;

            public int heuristicCost;

            public int parentTile;

            public int costNodeCost;

            public ushort status;
        }

        private class CostNodeComparer : IComparer<WorldShipPathFinder.CostNode>
        {
            public int Compare(WorldShipPathFinder.CostNode a, WorldShipPathFinder.CostNode b)
            {
                int cost = a.cost;
                int cost2 = b.cost;
                if (cost > cost2)
                {
                    return 1;
                }
                if (cost < cost2)
                {
                    return -1;
                }
                return 0;
            }
        }

        private FastPriorityQueue<WorldShipPathFinder.CostNode> openList;

        private WorldShipPathFinder.PathFinderNodeFast[] calcGrid;

        private ushort statusOpenValue = 1;

        private ushort statusClosedValue = 2;

        private const int SearchLimit = 500000;

        private static readonly SimpleCurve HeuristicStrength_DistanceCurve = new SimpleCurve
        {
            {
                new CurvePoint(30f, 1f),
                true
            },
            {
                new CurvePoint(40f, 1.3f),
                true
            },
            {
                new CurvePoint(130f, 2f),
                true
            }
        };

        private const float BestRoadDiscount = 0.5f;

        public WorldShipPathFinder()
        {
            this.calcGrid = new WorldShipPathFinder.PathFinderNodeFast[Find.WorldGrid.TilesCount];
            this.openList = new FastPriorityQueue<WorldShipPathFinder.CostNode>(new WorldShipPathFinder.CostNodeComparer());
        }

        public WorldPath FindPath(int startTile, int destTile, object requester, int ticksPerMove = -1, Func<float, bool> terminator = null)
        {
            if (startTile < 0)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to FindPath with invalid start tile ",
                    startTile,
                    ", caravan= ",
                    requester.ToString()
                }), false);
                return WorldPath.NotFound;
            }
            if (destTile < 0)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to FindPath with invalid dest tile ",
                    destTile,
                    ", caravan= ",
                    requester.ToString()
                }), false);
                return WorldPath.NotFound;
            }
            World world = Find.World;
            WorldGrid grid = world.grid;
            List<int> tileIDToNeighbors_offsets = grid.tileIDToNeighbors_offsets;
            List<int> tileIDToNeighbors_values = grid.tileIDToNeighbors_values;
            Vector3 normalized = grid.GetTileCenter(destTile).normalized;
            int num = 0;
            int num2 = (ticksPerMove < 1) ? 3300 : ticksPerMove;
            int num3 = this.CalculateHeuristicStrength(startTile, destTile);
            this.statusOpenValue += 2;
            this.statusClosedValue += 2;
            if (this.statusClosedValue >= 65435)
            {
                this.ResetStatuses();
            }
            this.calcGrid[startTile].knownCost = 0;
            this.calcGrid[startTile].heuristicCost = 0;
            this.calcGrid[startTile].costNodeCost = 0;
            this.calcGrid[startTile].parentTile = startTile;
            this.calcGrid[startTile].status = this.statusOpenValue;
            this.openList.Clear();
            this.openList.Push(new WorldShipPathFinder.CostNode(startTile, 0));
            while (this.openList.Count > 0)
            {
                WorldShipPathFinder.CostNode costNode = this.openList.Pop();
                if (costNode.cost == this.calcGrid[costNode.tile].costNodeCost)
                {
                    int tile = costNode.tile;
                    if (this.calcGrid[tile].status != this.statusClosedValue)
                    {
                        if (DebugViewSettings.drawPaths)
                        {
                            Find.WorldDebugDrawer.FlashTile(tile, (float)this.calcGrid[tile].knownCost / 375000f, this.calcGrid[tile].knownCost.ToString(), 50);
                        }
                        if (tile == destTile)
                        {
                            return this.FinalizedPath(tile);
                        }
                        if (num > 500000)
                        {
                            Log.Warning(string.Concat(new object[]
                            {
                                requester.ToString(),
                                " pathing from ",
                                startTile,
                                " to ",
                                destTile,
                                " hit search limit of ",
                                500000,
                                " tiles."
                            }), false);
                            return WorldPath.NotFound;
                        }
                        int num4 = (tile + 1 >= tileIDToNeighbors_offsets.Count) ? tileIDToNeighbors_values.Count : tileIDToNeighbors_offsets[tile + 1];
                        for (int i = tileIDToNeighbors_offsets[tile]; i < num4; i++)
                        {
                            int num5 = tileIDToNeighbors_values[i];
                            if (this.calcGrid[num5].status != this.statusClosedValue)
                            {
                                int num6 = (int)((float)num2);// * movementDifficulty[num5] * grid.GetRoadMovementDifficultyMultiplier(tile, num5, null));
                                int num7 = num6 + this.calcGrid[tile].knownCost;
                                ushort status = this.calcGrid[num5].status;
                                if ((status != this.statusClosedValue && status != this.statusOpenValue) || this.calcGrid[num5].knownCost > num7)
                                {
                                    Vector3 tileCenter = grid.GetTileCenter(num5);
                                    if (status != this.statusClosedValue && status != this.statusOpenValue)
                                    {
                                        float num8 = grid.ApproxDistanceInTiles(GenMath.SphericalDistance(tileCenter.normalized, normalized));
                                        this.calcGrid[num5].heuristicCost = Mathf.RoundToInt((float)num2 * num8 * (float)num3 * 0.5f);
                                    }
                                    int num9 = num7 + this.calcGrid[num5].heuristicCost;
                                    this.calcGrid[num5].parentTile = tile;
                                    this.calcGrid[num5].knownCost = num7;
                                    this.calcGrid[num5].status = this.statusOpenValue;
                                    this.calcGrid[num5].costNodeCost = num9;
                                    this.openList.Push(new WorldShipPathFinder.CostNode(num5, num9));
                                }

                            }
                        }
                        num++;
                        this.calcGrid[tile].status = this.statusClosedValue;
                        if (terminator != null && terminator((float)this.calcGrid[tile].costNodeCost))
                        {
                            return WorldPath.NotFound;
                        }
                    }
                }
            }
            Log.Warning(string.Concat(new object[]
            {
                requester.ToString(),
                " pathing from ",
                startTile,
                " to ",
                destTile,
                " ran out of tiles to process."
            }), false);
            return WorldPath.NotFound;
        }

        private WorldPath FinalizedPath(int lastTile)
        {
            WorldPath emptyWorldPath = Find.WorldPathPool.GetEmptyWorldPath();
            int num = lastTile;
            while (true)
            {
                WorldShipPathFinder.PathFinderNodeFast pathFinderNodeFast = this.calcGrid[num];
                int parentTile = pathFinderNodeFast.parentTile;
                int num2 = num;
                emptyWorldPath.AddNodeAtStart(num2);
                if (num2 == parentTile)
                {
                    break;
                }
                num = parentTile;
            }
            emptyWorldPath.SetupFound((float)this.calcGrid[lastTile].knownCost);
            return emptyWorldPath;
        }

        private void ResetStatuses()
        {
            int num = this.calcGrid.Length;
            for (int i = 0; i < num; i++)
            {
                this.calcGrid[i].status = 0;
            }
            this.statusOpenValue = 1;
            this.statusClosedValue = 2;
        }

        private int CalculateHeuristicStrength(int startTile, int destTile)
        {
            return 1;
            //return Mathf.RoundToInt(WorldShipPathFinder.HeuristicStrength_DistanceCurve.Evaluate(x));
        }
    }

}
