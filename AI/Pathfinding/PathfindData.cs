using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplePathfinder;

[CreateAssetMenu(fileName = "PathfindData", menuName = "ScriptableObjects/PathfindData")]
public class PathfindData : ScriptableObject {

    //public List<NodeGroup> nodeGroupList;
    public NodeGroupData[] nodeGroupArray; // Packed data

    public void Compress(List<NodeGroup> n) {

        //nodeGroupList = n;
        nodeGroupArray = new NodeGroupData[n.Count];

        for (int i = 0; i < nodeGroupArray.Length; i++) {

            nodeGroupArray[i] = new NodeGroupData(n[i]);

        }

    }

}
