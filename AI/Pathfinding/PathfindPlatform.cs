using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplePathfinder;

public class PathfindPlatform : MonoBehaviour
{

    public int gridDensity = 2;
    public NodeGroup nodeGroup { get; private set; }

    public void GenerateNodeGroup() {

        nodeGroup = new NodeGroup(transform.localPosition, transform.localScale, gridDensity);

    }

    public PathfindPlatform[] AllPlatforms() {

        if (Pathfinding.root != null)
            return Pathfinding.root.GetComponentsInChildren<PathfindPlatform>();

        return FindObjectsOfType(typeof(PathfindPlatform)) as PathfindPlatform[];

    }

    public void GenerateLinks() { // Additional functionality to add : height disconnection

        foreach (PathfindPlatform platform in AllPlatforms()) {

            if (platform.hideFlags != HideFlags.None || platform == this)
                continue;

            NodeGroup otherNodeGroup = platform.nodeGroup;

            if (nodeGroup.LinkGroups.Contains(otherNodeGroup))
                continue;

            float distance = Vector3.Distance(transform.position, platform.transform.position);

            if (distance <= (nodeGroup.Sqr + otherNodeGroup.Sqr)) {

                GenerateMultipleLinks(otherNodeGroup);

                nodeGroup.AddLinkGroup(otherNodeGroup);
                otherNodeGroup.AddLinkGroup(nodeGroup);

            }

        }

    }

    private void GenerateMultipleLinks(NodeGroup otherNodeGroup) {

        float nodeDist = Mathf.Min(nodeGroup.MaxNodeDistance, otherNodeGroup.MaxNodeDistance) * 1.44f;
        //float nodeDist = nodeGroup.MaxNodeDistance * 1.44f;

        foreach (Node node in nodeGroup.edges) {

            foreach (Node other in otherNodeGroup.edges) {

                float dist = Vector3.Distance(node.worldPosition, other.worldPosition);

                if (dist > nodeDist)
                    continue;

                if (Mathf.Abs(node.worldPosition.y - other.worldPosition.y) > 0.1f) { // Add jump threshold

                    // Maybe only node that is lower is tagged as jump

                    node.isJump = true;
                    other.isJump = true;

                }

                node.AddLinkNode(other);
                other.AddLinkNode(node);

            }

        }

    }

    public void BlockTest() {

        foreach (Node n in nodeGroup.nodes) {

            if (Physics.OverlapSphere(n.worldPosition + Vector3.up, 0.5f, StaticData.WORLD_LAYER).Length > 0) {

                n.isBlocked = true;

            }

        }

    }

    /*
    private void OnDrawGizmosSelected() {

        if (nodeGroup == null) {

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1f);

            return;

        }

        Gizmos.DrawWireCube(transform.position, Vector3.one * (nodeGroup.Sqr));

        foreach (Node n in nodeGroup.nodes) {

            Vector3 nodePosition = n.worldPosition;

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(nodePosition, 0.1f);

            Gizmos.color = Color.yellow;

            if (n.Adjacent == null)
                return;

            foreach (Node node in n.Adjacent) {

               // Vector2 gridOffset = node.gridPosition - n.gridPosition;
               // Vector3 offset = new Vector3(gridOffset.x, 0, gridOffset.y) * 0.25f;
               // Gizmos.DrawWireCube(nodePosition + offset, Vector3.one * 0.1f);

            }

            Gizmos.color = Color.green;

            foreach (Node linkNode in n.LinkNodes) {

                Gizmos.DrawLine(n.worldPosition, linkNode.worldPosition);

            }

        }

    }
    */

}
