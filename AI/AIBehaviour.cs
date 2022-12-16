using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleAI {

    public enum AIType {
        passive,
        agressive
    }

    public enum AIBehaviourType {
        idle,
        moveToRandomPosition,
        moveToPosition, // Includes path to and path away from
        moveToTarget,
        attackTarget,
        moveAwayFromTarget
    }

    [System.Serializable]
    public class AIBehaviour {

        public AIBehaviourType type;
        public AIBehaviourCondition[] conditions;
        public float behaviourRange;

        // Can include other stuff such as
        // index of specific attack,
        // skill to cast
        // condition (ex. <50% hp)
        // skip next action, or reset behaviour rotuine etc.

    }

}