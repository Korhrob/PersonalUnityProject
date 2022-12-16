using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimplePathfinder;
using SimpleAI;

public class MonsterBehaviour : MonoBehaviour
{

    private MonsterCharacter c;
    
    private List<Node> storedPath; // Minor optimization: store vector3 path instead(?)
    private Node lastNode;
    private Coroutine curPathfind, prevBehaviour;
    public Character Target;

    // DEBUG
    private Vector3 targetPos = Vector3.zero;
    private Vector3 lastPosition;
    private float pathfindStartTime;
    private int stuckCounter;

    private int curBehaviourIndex;

    private enum PathfindCondition {
        inRangeOfTarget
    }
    private List<PathfindCondition> pathfindConditions = new List<PathfindCondition>();
    private float behaviourRange = 1f;

    private void Awake() {

        c = GetComponent<MonsterCharacter>();

    }

    private void OnEnable() {

        Target = null;
        curBehaviourIndex = 0;
        StartCoroutine(BeginBehaviourDelay());

    }

    private bool TryGetNearestTarget(out Character nearestTarget) { // Add: optional conditions: frieldy target, can be self

        // Currently just gets first target in overlap sphere, Add:  distance check

        nearestTarget = null;
        Collider[] collisions = Physics.OverlapSphere(transform.position, 5f, StaticData.CHARACTER_LAYER);

        for (int i = 0; i < collisions.Length; i++) {

            Character hit = collisions[i].GetComponent<Character>();

            if (hit == null)
                continue;

            if (hit.IsFriendly(c.TeamID))
                continue;

            nearestTarget = hit;
            Target = hit;

            return true;

        }

        return false;

    }

    public void SetTarget(Character c) {

        Target = c;

    }

    public void EnterCombat() {

        if (c.CombatStatus == CombatStates.outOfCombat) {

            curBehaviourIndex = 0;
            c.CombatStatus = CombatStates.inCombat;

            BeginBehaviour();
            c.ToggleCombat();

        }

    }

    private IEnumerator BeginBehaviourDelay() {

        yield return new WaitForSeconds(0.1f);

        BeginBehaviour();

    }

    private void BeginBehaviour() {

        c.SetInputDirection(Vector3.zero);

        if (prevBehaviour != null)
            StopCoroutine(prevBehaviour);

        AIBehaviour[] curBehaviours;
        pathfindConditions = new List<PathfindCondition>();

        curBehaviours = c.monsterData.idleBehaviourRoutine;

        if (c.CombatStatus == CombatStates.inCombat) {

            if (Target == null) { // In combat but no target

                if (TryGetNearestTarget(out Target)) { // Can get nearby target

                    curBehaviours = c.monsterData.combatBehaviourRoutine;

                }

            } else {

                curBehaviours = c.monsterData.combatBehaviourRoutine;

            }

        }

        AIBehaviour curBehaviour = curBehaviours[curBehaviourIndex];
        behaviourRange = curBehaviour.behaviourRange;

        bool canExecute = true;

        // Check if curBehaviour has conditions
        foreach (AIBehaviourCondition condition in curBehaviour.conditions) {

            if (condition == null)
                continue;

            if (condition.CheckCondition(c)) {

                // If any condition is false, cant execute current behaviour

                canExecute = false;
                break;
                

            }

        }

        curBehaviourIndex++; // Condition should also check if it can be skipped when condition is not met

        if (curBehaviourIndex >= curBehaviours.Length)
            curBehaviourIndex = 0;

        if (canExecute)
            ExecuteBehaviour(curBehaviour);

    }

    private void ExecuteBehaviour(AIBehaviour behaviour) {

        switch (behaviour.type) {

            case AIBehaviourType.idle: prevBehaviour = StartCoroutine(Idle()); break;
            case AIBehaviourType.moveToRandomPosition: MoveToRandomPosition(); break;
            case AIBehaviourType.moveToTarget: MoveToTarget(); break;
            case AIBehaviourType.attackTarget: prevBehaviour = StartCoroutine(AttackTarget()); break; // Include attack range(?)
            case AIBehaviourType.moveAwayFromTarget: MoveAwayFromTarget(); break;

        }

    }


    private IEnumerator Idle() {

        float waitTime = Random.Range(1f, 1f + behaviourRange);

        if (c.monsterData.aiType == AIType.passive)
            yield return new WaitForSeconds(waitTime);
        else {

            // Agressive AI should try to look for targets whileidle

            float timeElapsed = 0f;

            while (timeElapsed < waitTime) {

                if (TryGetNearestTarget(out Target)) {

                    EnterCombat();
                    break;

                }

                yield return new WaitForSecondsRealtime(StaticData.PATHFIND_RATE);
                timeElapsed += StaticData.PATHFIND_RATE;

            }

        }

        BeginBehaviour();

    }

    private void MoveToRandomPosition() {

        // Might also need bias towards spawn direction to keep monster around its spawn location

        Vector3 targetPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        targetPosition.x += Random.Range(-behaviourRange, behaviourRange);
        targetPosition.z += Random.Range(-behaviourRange, behaviourRange);

        targetPos = targetPosition;

        Pathfinding p = GameManager.instance.pathfinding;
        List<NodeGroup> shortPath = p.ShortPath(transform.position, targetPosition);
        storedPath = p.FindPath(transform.position, targetPosition, shortPath);

        prevBehaviour = StartCoroutine(Pathfind());

    }

    private void MoveToTarget() {

        pathfindConditions.Add(PathfindCondition.inRangeOfTarget);

        Pathfinding p = GameManager.instance.pathfinding;
        List<NodeGroup> shortPath = p.ShortPath(transform.position, Target.transform.position);
        storedPath = p.FindPath(transform.position, Target.transform.position, shortPath);

        targetPos = Target.transform.position;

        prevBehaviour = StartCoroutine(Pathfind());

    }

    private void MoveAwayFromTarget() {

        Vector3 biasDirection = transform.position - Target.transform.position;

        Vector3 targetPosition = transform.position + (biasDirection * behaviourRange);
        targetPosition.x += Random.Range(-1f, 1f);
        targetPosition.z += Random.Range(-1f, 1f);

        targetPos = targetPosition;

        // Fast Path
        Pathfinding p = GameManager.instance.pathfinding;
        storedPath = new List<Node>();
        storedPath.Add(p.ClosestNode(targetPosition));

        // Full Pathfinding
        //pathfindConditions.Add(PathfindCondition.inRangeOfTarget);
        //Pathfinding p = GameManager.instance.pathfinding;
        //List<NodeGroup> shortPath = p.ShortPath(transform.position, targetPosition);
        //storedPath = p.FindPath(transform.position, targetPosition, shortPath);

        prevBehaviour = StartCoroutine(Pathfind());

    }

    private IEnumerator AttackTarget() {

        if (Target == null)
            if (TryGetNearestTarget(out Target) == false) {

                BeginBehaviour(); // Couldnt find a new target -> Reset Behaviour Loop
                yield break;

            }

        if (Vector3.Distance(transform.position, Target.transform.position) > behaviourRange) {

            BeginBehaviour(); // Target is not in attack range -> Reset Behaviour Loop
            yield break;

        }

        // -------------

        float timer = 0;
        float timeoutTime = 1f;

        // FIX: If monster is hit before stateManager is initialized, this causes an error
        //if (c.stateManager == null) 
        //    Debug.LogError("NULL STATE MANAGER");

        while (c.stateManager.CurState() is not DefaultState) {

            yield return new WaitForSecondsRealtime(StaticData.PATHFIND_RATE);
            timer += StaticData.PATHFIND_RATE;

            if (timer > timeoutTime)
                break;

        }

        Vector3 inputDirection = InputDirectionToTarget(Target.transform.position, 0.5f);

        c.SetInputDirection(inputDirection);

        yield return new WaitForSecondsRealtime(StaticData.PATHFIND_RATE);

        c.SetInputDirection(Vector2.zero);

        c.CurAttackIndex = 0; // Set 0 from behaviour to create more unique attack patterns
        c.stateManager.Attack();

        // Wait state change or timeout

        timer = 0;

        while (c.stateManager.CurState() is not AttackState) {

            yield return new WaitForSecondsRealtime(StaticData.PATHFIND_RATE);
            timer += StaticData.PATHFIND_RATE;

            if (timer > timeoutTime)
                break;

        }

        // Wait state reset or timeout

        timer = 0;

        while (c.stateManager.CurState() is not DefaultState) {

            yield return new WaitForSecondsRealtime(StaticData.PATHFIND_RATE);
            timer += StaticData.PATHFIND_RATE;

            if (timer > timeoutTime)
                break;

        }

        BeginBehaviour(); // Reset Behaviour Loop

    }

    private IEnumerator Pathfind() {

        pathfindStartTime = Time.time;

        if (lastNode != null)
            lastNode.numUsers--;

        if (storedPath == null) {

            storedPath = new List<Node>();

            if (lastNode != null)
                storedPath.Add(lastNode);

        }

        Node curNode = storedPath[0];
        curNode.numUsers++;
        lastNode = curNode;

        while (storedPath.Count > 0) {

            if (InteruptCondition()) {

                break;

            }

            if (Time.time - pathfindStartTime > 5f) {

                break;

            }

            if (Vector3.Distance(transform.position, curNode.worldPosition) <= 1f || IsStuck()) {

                pathfindStartTime = Time.time; // Reset timeout

                lastNode = curNode;         // Set last node
                storedPath[0].numUsers--;
                storedPath.RemoveAt(0);

                if (storedPath.Count == 0)
                    break;

                curNode = storedPath[0];    // Set next node
                curNode.numUsers++;         // Occupy next node

            }

            c.SetInputDirection(InputDirectionToTarget(curNode.worldPosition, 0.2f));
            // Add contextual input (make ai weight its inputs based on whats around it)
            // For example if there's a pit on left side, input will skew slightly towards right

            yield return new WaitForSeconds(StaticData.PATHFIND_RATE);

        }

        foreach (Node node in storedPath)
            node.numUsers--;

        lastNode.numUsers++;

        c.SetInputDirection(Vector3.zero);
        yield return new WaitForSeconds(StaticData.PATHFIND_RATE); // Wait a few frames 

        curPathfind = null;

        BeginBehaviour();

    }

    private bool InteruptCondition() {

        if (pathfindConditions.Contains(PathfindCondition.inRangeOfTarget)) {

            if (Vector3.Distance(transform.position, Target.transform.position) < behaviourRange) {

                return true;

            }

        }

        return false;

    }

    private bool IsStuck() {

        // Character has been stuck for 20 ticks

        if (transform.position == lastPosition) {

            if (stuckCounter > 20) {

                stuckCounter = 0;
                return true;

            }

            stuckCounter++;

        } else
            stuckCounter = 0;

        lastPosition = transform.position;

        return false;

    }

    private Vector3 InputDirectionToTarget(Vector3 _targetPos, float _threshold) {

        Vector3 _inputDirection = Vector3.zero;

        float xComparer = _targetPos.x - transform.position.x;
        float xTreshold = Mathf.Abs(xComparer);

        if (xTreshold > _threshold) {

            if (xComparer < 0f)
                _inputDirection.x = -1;

            if (xComparer > 0f)
                _inputDirection.x = 1;

        }

        float yComparer = _targetPos.z - transform.position.z;
        float yTreshold = Mathf.Abs(yComparer);

        if (yTreshold > _threshold) {

            if (yComparer < 0f)
                _inputDirection.z = -1;

            if (yComparer > 0f)
                _inputDirection.z = 1;

        }

        return _inputDirection;

    }

    private void OnDrawGizmosSelected() {

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(targetPos, Vector3.one);

        if (storedPath != null) {

            Gizmos.color = Color.green;

            for (int i = 0; i < storedPath.Count -1; i++) {

                Gizmos.DrawLine(storedPath[i].worldPosition, storedPath[i + 1].worldPosition);

            }

        }

    }

}
