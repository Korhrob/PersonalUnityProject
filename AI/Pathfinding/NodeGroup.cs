using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SimplePathfinder {

    public class NodeGroup {

        // TO DO: Variable grid Size (for ex. if width and height dont match, grid should mattch rectangle shape)
        // TO DO : technically center, size, sqr, maxnodedistance and grid density are not required. We can use temporary values and discard them after generation.

        public Vector3 Center { get; private set; }
        public Vector3 Size { get; private set; }
        public float Sqr { get; private set; }
        public float MaxNodeDistance { get; private set; }
        public int GridDensity { get; private set; }
        public int id;
        public List<Node> nodes;
        public List<NodeGroup> LinkGroups { get; private set;}
        public List<Node> edges = new List<Node>();

        public NodeGroup(Vector3 _position, Vector3 _scale, int _GridDensity) {

            Center = _position;      //m.bounds.Center;
            Size = _scale;           //m.bounds.Size;
            Sqr = (Size.x + Size.y) * 0.25f;
            GridDensity = _GridDensity;

            nodes = GenerateNodeGrid();
            LinkGroups = new List<NodeGroup>();

        }

        public NodeGroup(NodeGroupData packedData) {

            id = packedData.id;
            Center = packedData.center;
            nodes = new List<Node>();
            LinkGroups = new List<NodeGroup>();

            foreach (NodeData n in packedData.nodesData) {

                Node unpack = new Node(n);
                nodes.Add(unpack);

            }

        }

        public List<Node> GenerateNodeGrid() {

            List<Node> tNodeList = new List<Node>();
            Dictionary<Vector2, Node> tNodeGrid = new Dictionary<Vector2, Node>();

            Vector2 dividedSize = new Vector2(Size.x, Size.y); // (GridDensity * 2f);
            Vector2 density = new Vector2(Size.x, Size.y);

            float largerLength = Mathf.Max(Size.x, Size.z);
            int xDensity = Mathf.RoundToInt(GridDensity * (density.x / largerLength));
            int yDensity = Mathf.RoundToInt(GridDensity * (density.y / largerLength));

            dividedSize /= (GridDensity * 2);

            // Should clamp these
            dividedSize.x *= (largerLength / density.x);
            dividedSize.y *= (largerLength/ density.y);

            Vector3 offset = dividedSize * 0.5f;

            MaxNodeDistance = (dividedSize.x + dividedSize.y) / 2;
            Sqr += MaxNodeDistance;

            for (int x = -xDensity; x < xDensity; x++) {

                for (int y = -yDensity; y < yDensity; y++) {

                    Vector2 gridPosition = new Vector2(x, y);
                    Vector3 worldPosition = Center + new Vector3(x * dividedSize.x + offset.x, 0, y * dividedSize.y + offset.y);

                    Node newNode = new Node(gridPosition, worldPosition);

                    tNodeGrid.Add(gridPosition, newNode);
                    tNodeList.Add(newNode);

                }

            }

            ConnectNeighbours(tNodeGrid, xDensity, yDensity);

            return tNodeList;

        }

        public void ConnectNeighbours(Dictionary<Vector2, Node> nodeGrid, int xDensity, int yDensity) {

            // Populate neighbours
            for (int x = -xDensity; x < xDensity; x++) {

                for (int y = -yDensity; y < yDensity; y++) {

                    Node curNode = nodeGrid[new Vector2(x, y)];
                    Node n;

                    if (x > -xDensity) {
                        // Not left most node

                        n = nodeGrid[new Vector2(x -1, y)]; // Center Left
                        curNode.AddNeighbour(n);

                        if (y > -yDensity) {

                            n = nodeGrid[new Vector2(x - 1, y - 1)]; // Bottom Left
                            curNode.AddNeighbour(n);

                        }

                        if (y < yDensity - 1) {

                            n = nodeGrid[new Vector2(x - 1, y + 1)]; // Top Left
                            curNode.AddNeighbour(n);

                        }

                    }

                    if (x < xDensity - 1) {
                        // Not right most node

                        n = nodeGrid[new Vector2(x + 1, y)]; // Center Right
                        curNode.AddNeighbour(n);

                        if (y > -yDensity) {

                            n = nodeGrid[new Vector2(x + 1, y - 1)]; // BottomRight
                            curNode.AddNeighbour(n);

                        }

                        if (y < yDensity - 1) {

                            n = nodeGrid[new Vector2(x + 1, y + 1)]; // TopRight
                            curNode.AddNeighbour(n);

                        }

                    }

                    if (y > -yDensity) {
                        // Not bottom node

                        n = nodeGrid[new Vector2(x, y - 1)]; // Bottom Center
                        curNode.AddNeighbour(n);

                    }

                    if (y < yDensity - 1) {
                        // Not top node
                        n = nodeGrid[new Vector2(x, y + 1)];  // Top Center
                        curNode.AddNeighbour(n);

                    }

                }

            }

            for (int x = -xDensity; x < xDensity; x++) {

                for (int y = -yDensity; y < yDensity; y++) {

                    if (x == -xDensity || x == xDensity - 1 || y == -yDensity || y == yDensity - 1) {

                        //nodeGrid[new Vector2(x, y)].isEdge = true;
                        edges.Add(nodeGrid[new Vector2(x, y)]);

                    }

                }

            }

        }

        public void AddLinkGroup(NodeGroup nodeGroup) {

            if (LinkGroups.Contains(nodeGroup))
                return;

            LinkGroups.Add(nodeGroup);

        }

        public void Reconnect(NodeGroup nodeGroup) {

            AddLinkGroup(nodeGroup);

        }

        // Pathfinding

        public NodeGroup cameFromNodeGroup;
        public int gCost, hCost, fCost;

        public void CalculateFCost() {

            fCost = gCost + hCost;

        }

    }

    public class Node {

        //public Vector2 gridPosition;
        public int id;
        public Vector3 worldPosition;
        public List<Node> Adjacent { get; private set; }
        public List<Node> LinkNodes { get; private set; }
        public List<Node> AllConnected { get; private set; }
        public List<Node> overDraw; // for editor only
        public bool isJump;
        public bool isBlocked;

        public Node(Vector2 gridPosition, Vector3 worldPosition) {

            //this.gridPosition = gridPosition;
            this.worldPosition = worldPosition;

            Adjacent = new List<Node>();
            LinkNodes = new List<Node>();
            AllConnected = new List<Node>();

            // Editor
            overDraw = new List<Node>();

        }

        public Node(NodeData packedData) {

            this.id = packedData.id;
            this.worldPosition = packedData.worldPosition;
            this.isJump = packedData.isJump;
            this.isBlocked = packedData.isBlocked;

            AllConnected = new List<Node>();

            // Editor
            overDraw = new List<Node>();

        }

        public void AddNeighbour(Node n) {

            if (Adjacent.Contains(n))
                return;

            Adjacent.Add(n);
            AddConnection(n); // AllConnected.Add(n);

        }

        public void AddLinkNode(Node n) {

            if (LinkNodes.Contains(n))
                return;

            LinkNodes.Add(n);
            AddConnection(n); //AllConnected.Add(n);

        }

        public void AddConnection(Node n) {

            // Reduce Editor Overdraw
            if (n.overDraw.Contains(this) == false) // if n has not listed this as overdraw
                overDraw.Add(n);

            if (AllConnected.Contains(n)) {

                // Connection already exists
                return;

            }

            AllConnected.Add(n);

        }

        public void Reconnect(Node n) {

            AddConnection(n);

        }

        // Pathfinding

        public Node cameFromNode;
        public int gCost, hCost, fCost;
        public int numUsers;

        public void CalculateFCost() {

            fCost = gCost + hCost;

        }

    }

}