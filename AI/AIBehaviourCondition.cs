using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleAI;

public abstract class AIBehaviourCondition : ScriptableObject {

    public abstract bool CheckCondition(Character c);

}

[CreateAssetMenu(fileName = "AIBehaviourCondition", menuName = "ScriptableObjects/AI/Condition/HPGreaterThan")]
public class HealthIsGreaterThan : AIBehaviourCondition {

    [Range(0f, 1f)]
    public float percentage; // 0 = 0%, 1 = 100%

    public override bool CheckCondition(Character c) {

        if (c.CharacterStatus.HealthPercentage() >= percentage)
            return true;

        return false;

    }

}

[CreateAssetMenu(fileName = "AIBehaviourCondition", menuName = "ScriptableObjects/AI/Condition/HPLowerThan")]
public class HealthIsLessThan : AIBehaviourCondition {

    [Range(0f, 1f)]
    public float percentage; // 0 = 0%, 1 = 100%

    public override bool CheckCondition(Character c) {

        if (c.CharacterStatus.HealthPercentage() >= percentage)
            return true;

        return false;

    }

}