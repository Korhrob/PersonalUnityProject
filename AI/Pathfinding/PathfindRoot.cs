using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplePathfinder;

public class PathfindRoot : MonoBehaviour {

    private void Awake() {

        Pathfinding.root = gameObject;

    }

}
