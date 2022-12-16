using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimplePathfinder {

    public class Pathfinding {

        public static GameObject root;
        public List<NodeGroup> pathfindData;

        public Pathfinding(List<NodeGroup> data) {

            pathfindData = data;

        }

        public Pathfinding(PathfindData packedData) { // Unpack

            pathfindData = Unpack(packedData);

        }

        public static List<NodeGroup> Unpack(PathfindData packedData) {

            List<NodeGroup> tList = new List<NodeGroup>();
            Dictionary<int, NodeGroup> nodeToDataGroup = new Dictionary<int, NodeGroup>();
            Dictionary<int, Node> nodeToData = new Dictionary<int, Node>();

            List<NodeData> nodeData = new List<NodeData>();

            foreach (NodeGroupData packedDataGroup in packedData.nodeGroupArray) {

                NodeGroup nodeGroup = new NodeGroup(packedDataGroup);
                tList.Add(nodeGroup);

                nodeToDataGroup.Add(packedDataGroup.id, nodeGroup);

                foreach (Node node in nodeGroup.nodes)
                    nodeToData.Add(node.id, node);

                foreach (NodeData packedNode in packedDataGroup.nodesData)
                    nodeData.Add(packedNode);

            }

            foreach (NodeGroupData packedDataGroup in packedData.nodeGroupArray) {

                for (int i = 0; i < packedDataGroup.linkGroups.Length; i++) {

                    nodeToDataGroup[packedDataGroup.id].Reconnect(nodeToDataGroup[packedDataGroup.linkGroups[i]]);

                }

            }

            foreach (NodeData packedNode in nodeData) {

                for (int i = 0; i < packedNode.allConnected.Length; i++) {

                    nodeToData[packedNode.id].Reconnect(nodeToData[packedNode.allConnected[i]]);

                }

            }

            return tList;

        }

        private List<NodeGroup> openGroups, closedGroups;
        private List<Node> openNodes, closedNodes;

        // Write SpeedPath() function that only pathfinds through chunks
        // Cuts down # nodes used for final pathfinding by a lot

        public List<NodeGroup> ShortPath(Vector3 startPosition, Vector3 endPosition) { // shortpath a* nodegroups only

            // Should also take into account how many available nodes a group has
            // for ex. NodeGroup A might be closest, but has 0 available nodes to use
            // thus cant be used to a* pathfind using shortpath

            NodeGroup startNodeGroup = ClosestNodeGroup(startPosition);
            NodeGroup endNodeGroup = ClosestNodeGroup(endPosition);

            openGroups = new List<NodeGroup>() { startNodeGroup };
            closedGroups = new List<NodeGroup>();

            foreach (NodeGroup nodeGroup in pathfindData) {

                nodeGroup.gCost = int.MaxValue;
                nodeGroup.CalculateFCost();
                nodeGroup.cameFromNodeGroup = null;

            }

            startNodeGroup.gCost = 0;
            startNodeGroup.hCost = CalculateDistance(startNodeGroup, endNodeGroup);
            startNodeGroup.CalculateFCost();

            while (openGroups.Count > 0) {

                NodeGroup currentGroup = GetLowestFCostNodeGroup(openGroups);

                if (currentGroup == endNodeGroup) {

                    return CalculatePath(endNodeGroup);

                }

                openGroups.Remove(currentGroup);
                closedGroups.Add(currentGroup);

                foreach (NodeGroup neighbour in currentGroup.LinkGroups) {

                    if (closedGroups.Contains(neighbour))
                        continue;

                    int tentativeGCost = Mathf.RoundToInt(currentGroup.gCost + CalculateDistance(currentGroup, neighbour));
                    if (tentativeGCost < neighbour.gCost) {

                        neighbour.cameFromNodeGroup = currentGroup;
                        neighbour.gCost = tentativeGCost;
                        neighbour.hCost = CalculateDistance(neighbour, endNodeGroup);
                        neighbour.CalculateFCost();

                        if (!openGroups.Contains(neighbour))
                            openGroups.Add(neighbour);

                    }

                }

            }

            Debug.Log("No shortpath found");
            return null;

        }

        public List<Node> FindPath(Vector3 startPosition, Vector3 endPosition) { // Default full a*

            Node startNode = ClosestNode(startPosition);
            Node endNode = ClosestNode(endPosition);

            openGroups = new List<NodeGroup>();
            closedGroups = new List<NodeGroup>();

            openNodes = new List<Node>() { startNode };
            closedNodes = new List<Node>();

            foreach (NodeGroup nodeGroup in pathfindData) {

                foreach (Node node in nodeGroup.nodes) {

                    node.gCost = int.MaxValue;
                    node.CalculateFCost();
                    node.cameFromNode = null;

                    if (node.isBlocked)
                        closedNodes.Add(node);

                }

            }

            startNode.gCost = 0;
            startNode.hCost = CalculateDistance(startNode, endNode);
            startNode.CalculateFCost();

            while (openNodes.Count > 0) {

                Node currentNode = GetLowestFCostNode(openNodes);

                if (currentNode == endNode) { // || CalculateDistance(currentNode, endNode) < 1.44f

                    return CalculatePath(endNode);

                }

                openNodes.Remove(currentNode);
                closedNodes.Add(currentNode);

                foreach(Node neighbour in currentNode.AllConnected) {

                    if (closedNodes.Contains(neighbour)) 
                        continue;

                    int tentativeGCost = Mathf.RoundToInt(currentNode.gCost + CalculateDistance(currentNode, neighbour) + currentNode.numUsers);
                    if (tentativeGCost < neighbour.gCost) {

                        neighbour.cameFromNode = currentNode;
                        neighbour.gCost = tentativeGCost;
                        neighbour.hCost = CalculateDistance(neighbour, endNode);
                        neighbour.CalculateFCost();

                        if (!openNodes.Contains(neighbour))
                            openNodes.Add(neighbour);

                    }

                }

            }

            return null;

        }

        public List<Node> FindPath(Vector3 startPosition, Vector3 endPosition, List<NodeGroup> shortPath) { // a* from shortpath

            // Getting closest node in closest nodegroup causes issues where two nodegroups align
            // To avoid this lets try to just get closest node (which should in theory be included in shortpath)
            // technically it doesnt even matter if the start & end node are not in the short path

            Node startNode = ClosestNode(startPosition); //ClosestNodeIn(startPosition, ClosestNodeGroupIn(startPosition, shortPath));
            Node endNode = ClosestNode(endPosition); //ClosestNodeIn(endPosition, ClosestNodeGroupIn(endPosition, shortPath));

            openGroups = new List<NodeGroup>();
            closedGroups = new List<NodeGroup>();

            openNodes = new List<Node>() { startNode };
            closedNodes = new List<Node>();

            foreach (NodeGroup nodeGroup in shortPath) {

                foreach (Node node in nodeGroup.nodes) {

                    node.gCost = int.MaxValue;
                    node.CalculateFCost();
                    node.cameFromNode = null;

                    if (node.isBlocked)
                        closedNodes.Add(node);

                }

            }

            startNode.gCost = 0;
            startNode.hCost = CalculateDistance(startNode, endNode);
            startNode.CalculateFCost();

            while (openNodes.Count > 0) {

                Node currentNode = GetLowestFCostNode(openNodes);

                if (currentNode == endNode) { // || CalculateDistance(currentNode, endNode) < 1.44f

                    return CalculatePath(endNode);

                }

                openNodes.Remove(currentNode);
                closedNodes.Add(currentNode);

                foreach (Node neighbour in currentNode.AllConnected) {

                    if (closedNodes.Contains(neighbour))
                        continue;

                    int tentativeGCost = Mathf.RoundToInt(currentNode.gCost + CalculateDistance(currentNode, neighbour) + currentNode.numUsers);
                    if (tentativeGCost < neighbour.gCost) {

                        neighbour.cameFromNode = currentNode;
                        neighbour.gCost = tentativeGCost;
                        neighbour.hCost = CalculateDistance(neighbour, endNode);
                        neighbour.CalculateFCost();

                        if (!openNodes.Contains(neighbour))
                            openNodes.Add(neighbour);

                    }

                }

            }

            return null;

        }

        private List<Node> TwoPointPath(Node startNode, Node endNode) {

            List<Node> tPath = new List<Node>() {
                startNode, 
                endNode,
            };

            return tPath;

        }

        private List<Node> CalculatePath(Node endNode) {

            List<Node> path = new List<Node>();
            path.Add(endNode);
            Node currentNode = endNode;

            while (currentNode.cameFromNode != null) {

                path.Add(currentNode.cameFromNode);
                currentNode = currentNode.cameFromNode;

            }

            path.Reverse();

            return path;

        }

        private List<NodeGroup> CalculatePath(NodeGroup endNodeGroup) {

            List<NodeGroup> path = new List<NodeGroup>();
            path.Add(endNodeGroup);
            NodeGroup currentNodeGroup = endNodeGroup;

            while (currentNodeGroup.cameFromNodeGroup != null) {

                path.Add(currentNodeGroup.cameFromNodeGroup);
                currentNodeGroup = currentNodeGroup.cameFromNodeGroup;

            }

            path.Reverse();

            return path;

        }

        private int CalculateDistance(Node a, Node b) {

            return Mathf.RoundToInt(Vector3.Distance(a.worldPosition, b.worldPosition));

        }

        private int CalculateDistance(NodeGroup a, NodeGroup b) {

            return Mathf.RoundToInt(Vector3.Distance(a.Center, b.Center));

        }

        private Node GetLowestFCostNode(List<Node> pathNodeList) {

            Node lowestFCostNode = pathNodeList[0];

            for (int i = 1; i < pathNodeList.Count; i++) {

                if (pathNodeList[i].fCost < lowestFCostNode.fCost) {

                    lowestFCostNode = pathNodeList[i];

                }

            }

            return lowestFCostNode;

        }

        private NodeGroup GetLowestFCostNodeGroup(List<NodeGroup> nodeGroupList) {

            NodeGroup lowestFCostNode = nodeGroupList[0];

            for (int i = 1; i < nodeGroupList.Count; i++) {

                if (nodeGroupList[i].fCost < lowestFCostNode.fCost) {

                    lowestFCostNode = nodeGroupList[i];

                }

            }

            return lowestFCostNode;

        }

        public NodeGroup ClosestNodeGroup(Vector3 fromPosition) {

            float dist = Mathf.Infinity;
            NodeGroup closest = null;

            foreach (NodeGroup nodeGroup in pathfindData) {

                float distance = Vector3.Distance(fromPosition, nodeGroup.Center);

                if (distance < dist) {

                    closest = nodeGroup;
                    dist = distance;

                }

            }

            return closest;

        }

        public NodeGroup ClosestNodeGroupIn(Vector3 fromPosition, List<NodeGroup> nodeGroups) {

            float dist = Mathf.Infinity;
            NodeGroup closest = null;

            foreach (NodeGroup nodeGroup in nodeGroups) {

                float distance = Vector3.Distance(fromPosition, nodeGroup.Center);

                if (distance < dist) {

                    closest = nodeGroup;
                    dist = distance;

                }

            }

            return closest;

        }

        public Node ClosestNode(Vector3 fromPosition) {

            NodeGroup nodeGroup = ClosestNodeGroup(fromPosition);

            float dist = Mathf.Infinity;
            Node closest = null;

            foreach (Node node in nodeGroup.nodes) {

                if (node.isBlocked) // Node is blocked, cant be closest
                    continue;

                float distance = Vector3.Distance(fromPosition, node.worldPosition);

                if (distance < dist) {

                    closest = node;
                    dist = distance;

                }

            }

            return closest;

        }

        public Node ClosestNodeIn(Vector3 fromPosition, NodeGroup nodeGroup) {

            //NodeGroup nodeGroup = ClosestNodeGroupIn(fromPosition, nodeGroups);

            float dist = Mathf.Infinity;
            Node closest = null;

            foreach (Node node in nodeGroup.nodes) {

                if (node.isBlocked) // Node is blocked, cant be closest
                    continue;

                float distance = Vector3.Distance(fromPosition, node.worldPosition);

                if (distance < dist) {

                    closest = node;
                    dist = distance;

                }

            }

            return closest;

        }

        public static Node ClosestNode(List<NodeGroup> nodeGroups, Vector3 fromPosition) {

            float dist = Mathf.Infinity;
            NodeGroup closestGroup = null;

            foreach (NodeGroup nodeGroup in nodeGroups) {

                float distance = Vector3.Distance(fromPosition, nodeGroup.Center);

                if (distance < dist) {

                    closestGroup = nodeGroup;
                    dist = distance;

                }

            }

            if (closestGroup == null)
                return null;

            dist = Mathf.Infinity;
            Node closestNode = null;

            foreach (Node node in closestGroup.nodes) {

                float distance = Vector3.Distance(fromPosition, node.worldPosition);

                if (distance < dist) {

                    closestNode = node;
                    dist = distance;

                }

            }

            return closestNode;

        }

        public static List<NodeGroup> GeneratePathfinding() {

            Debug.LogWarning("GeneratePathfinding");

            // Try find root

            List<NodeGroup> nodeGroups = new List<NodeGroup>();
            PathfindPlatform[] all = Object.FindObjectsOfType(typeof(PathfindPlatform)) as PathfindPlatform[];

            foreach (PathfindPlatform platform in all) {

                if (platform.hideFlags != HideFlags.None)
                    continue;

                platform.GenerateNodeGroup();

                NodeGroup nodeGroup = platform.nodeGroup;
                nodeGroups.Add(nodeGroup);

            }

            foreach (PathfindPlatform platform in all) {

                if (platform.hideFlags != HideFlags.None)
                    continue;

                platform.GenerateLinks();
                platform.BlockTest();

            }

            return nodeGroups;

        }

        public static int TotalNodes(List<NodeGroup> n) {

            int i = 0;

            foreach (NodeGroup nodeGroup in n) {

                i += nodeGroup.nodes.Count;

            }

            return i;

        }

        public static int TotalConnections(List<NodeGroup> n) {

            int i = 0;

            foreach (NodeGroup nodeGroup in n) {

                foreach(Node node in nodeGroup.nodes)
                    i += node.AllConnected.Count;

            }

            return i;

        }

        public static int ReducedConnections(List<NodeGroup> n) {

            int i = 0;

            foreach (NodeGroup nodeGroup in n) {

                foreach (Node node in nodeGroup.nodes) {

                    i += node.AllConnected.Count;
                    i -= node.overDraw.Count;

                }
                    

            }

            return i;

        }

        public static void IndexNodes(List<NodeGroup> list) {

            int index = 0;

            for (int i = 0; i < list.Count; i++) {

                NodeGroup nGroup = list[i];
                nGroup.id = i;

                for (int b = 0; b < nGroup.nodes.Count; b++) {

                    Node n = nGroup.nodes[b];
                    n.id = index;
                    index++;

                }

            }

        }

    }

}