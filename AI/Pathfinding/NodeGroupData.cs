using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplePathfinder;

[System.Serializable]
public class NodeGroupData {

    public int id;
    public Vector3 center;
    public int[] linkGroups;
    public NodeData[] nodesData;

    public NodeGroupData(NodeGroup nGroup) {

        id = nGroup.id;
        center = nGroup.Center;
        nodesData = new NodeData[nGroup.nodes.Count];

        for (int i = 0; i < nodesData.Length; i++) {

            nodesData[i] = new NodeData(nGroup.nodes[i]);

        }

        linkGroups = new int[nGroup.LinkGroups.Count];

        for (int i = 0; i < linkGroups.Length; i++) {

            linkGroups[i] = nGroup.LinkGroups[i].id;

        }

    }

}

[System.Serializable]
public class NodeData {

    public int id;
    public Vector3 worldPosition;
    public int[] allConnected; // AllConnecter.id > this
    public bool isJump, isBlocked; // Make this enum

    public NodeData(Node n) {

        this.id = n.id;
        this.worldPosition = n.worldPosition;
        this.isJump = n.isJump;
        this.isBlocked = n.isBlocked;
        allConnected = new int[n.AllConnected.Count];

        for (int i = 0; i < allConnected.Length; i++)
            allConnected[i] = n.AllConnected[i].id;

    }

}