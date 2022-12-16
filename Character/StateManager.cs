using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateManager {

    private Character c; // Character ref
    private Transform t; // Characters transform ref (not needed)

    private int curFrame;
    private CharacterState curState;
    private CharacterState nextState;
    private CharacterState bufferState;

    private bool extend;
    private int bufferFrames;

    public StateManager(Character _c, Transform _t) {

        c = _c;
        t = _t;

        curState = new DefaultState(c, 60, 1);
        curFrame = 0;

    }

    public void NewState(CharacterState _newState) {

        if (nextState != null)
            if (_newState.OverridePriority < nextState.OverridePriority)
                return;

        nextState = _newState;

        
        if (curState.CanOverride) {

            // If current state can be overriden start nextstate
            SetState(nextState);

        }

    }

    public void ForceState(CharacterState _newState) {

        if (curState.CanOverride == false) {

            // FIX: state buffering
            //bufferState = _newState;
            //bufferFrames = 8;
            return;

        }

        SetState(_newState);

    }

    public void ForceNextState(CharacterState _nextState) {

        _nextState.OverridePriority = 1;
        nextState = _nextState;


    }

    public void SetState(CharacterState _state, bool _sendFlag = true) {

        curState.OnExitState();

        curState = _state;
        curState.OnEnterState();
        curFrame = 0;

        nextState = null;
        extend = false;


        if (_sendFlag) {

            // Minor optimization: dont send state update if current and previous states are both default (idle state)
            // Saves about 1 message (ushort, ushort, int, int) per character per second per client
            c.SendState(CurStateToEnum(), _state.TotalFrames);
            

        }

    }

    public void CountFrames() {

        ClearBufferState();

        if (extend) {

            curState.Extend();
            extend = false;

        }

        if (curState.CancelState) {

            EndState();
            return;

        }

        if (curFrame == curState.ActionFrame) {

            curState.Execute();

        }

        curState.EveryFrame(curFrame);
        curFrame++;

        if (curFrame == curState.TotalFrames) {

            curState.LastFrame();

        }

        if (curFrame >= curState.TotalFrames) {

            if (c.IsLocal == false) {

                // To predict what remote clients are trying to do, we are assuming they will stay in the same state so we dont end state
                return;

            }

            EndState();

        }
        
    }

    public void EndState() {

        bool sendFlag = true;

        if (nextState == null) { // Create new default state if no new state exists

            nextState = new DefaultState(c, 60, 4);

            // if current state is also default state, we dont have to send this state update to server

            if (curState is DefaultState)
                sendFlag = false;

        }

        SetState(nextState, sendFlag);

    }

    public CharacterState GetNextState() {

        return nextState;

    }

    public CharacterState CurState() {

        return curState;

    }

    public void Attack() {

        if (TryRecoveryAttack())
            return;

        NewState(new AttackState(c, 30, 16));

    }

    public void SecondaryAttack(int _index) {

        if (TryDashAttack())
            return;

        if (TryRecoveryAttack())
            return;

        if (_index == 0) {                                  // Default secondary attack

            if (curState is SecondaryAttackState secondary) // Already executing default
                if (secondary.CurIndex == 0)                // Cur index is 0
                    _index++;                               // increase index

        }

        NewState(new SecondaryAttackState(c, 30, 16, _index));

    }

    public void GroundAttack() {

        // Ground attack currently stored in secondary index 3
        NewState(new SecondaryAttackState(c, 30, 16, 3));

    }

    private bool TryDashAttack() {

        if (curState is DashState dashState) {

            if (curFrame < 20) {

                // Dash attack currently stored in secondary index 2
                NewState(new SecondaryAttackState(c, 30, 16, 2, false));
                dashState.attackCancel = true;

                return true;

            }

        }

        return false;

    }

    private bool TryRecoveryAttack() {

        if (nextState is RecoveryState)
            if (c.characterData.secondaryAttacks.Length >= 4) {

                // Recovery Attack currently stored in secondary index 4
                ForceNextState(new SecondaryAttackState(c, 50, 30, 4)); 
                return true;

            }

        return false;

    }

    public void Delay(int _frames) {

        NewState(new DelayState(c, _frames, 1));

    }

    public void Dash(int _frames) {

        if (!curState.CanDashCancel) { // Dash buffering

            bufferState = new DashState(c, _frames, 4);
            bufferFrames = 8;
            return;

        }

        SetState(new DashState(c, _frames, 4));

    }

    public void DashCancel() {

        SetState(new DashCancelState(c, 24 , 1));

    }

    public void Move() {

        if (curState is MoveState) {

            // Have to extend state inside CountFrames method since this method can be called multiple times per frame

            extend = true;
            return;

        }

        if (curState is DashState) { // Dash Cancel

            if (c.InvertInput(c.FaceDirection) && curFrame >= 8)
                SetState(new DashCancelState(c, 24, 1));

        }

        NewState(new MoveState(c, 4, 1));

    }

    public void Knockup() {

        SetState(new KnockupState(c, 30, 1));
        NewState(new RecoveryState(c, 30, 1)); // Queue next action (Recovery)

    }

    public void Recovery() {

        NewState(new RecoveryState(c, 30, 1));

    }

    public void Flinch(int _frames = 8) {

        if (curState is KnockupState)
            return;

        SetState(new FlinchState(c, _frames, 1));

    }

    public void CastSkill(Active skill) {

        NewState(new CastState(c, 60, 30, skill));

    }

    public CharacterStates CurStateToEnum() {

        // TO DO: Cleanup
        // 2ndary and cast state derive from attackstate

        if (curState is SecondaryAttackState)
            return CharacterStates.SecondaryAttack;

        if (curState is CastState)
            return CharacterStates.Cast;

        switch(curState) {
            case MoveState: return CharacterStates.Move;
            case DashState: return CharacterStates.Dash;
            case DashCancelState: return CharacterStates.DashCancel;
            case AttackState: return CharacterStates.Attack;
            case DelayState: return CharacterStates.Delay;
            case KnockupState: return CharacterStates.Knockup;
            case RecoveryState: return CharacterStates.Recovery;

        }

        return CharacterStates.Default;

    }

    private CharacterState EnumToState(CharacterStates _state, int _animation, int _frames, int _skillID = 0) {

        switch (_state) {

            case CharacterStates.Move: return new MoveState(c, _frames, 1);
            case CharacterStates.Dash: return new DashState(c, _frames, 1);
            case CharacterStates.DashCancel: return new DashCancelState(c, _frames, 1);
            case CharacterStates.Attack: return new AttackState(c, _frames, 16);
            case CharacterStates.SecondaryAttack: return new SecondaryAttackState(c, _frames, 16, _animation);
            case CharacterStates.Delay: return new DelayState(c, _frames, 1);
            case CharacterStates.Knockup: return new KnockupState(c, _frames, 1);
            case CharacterStates.Recovery: return new RecoveryState(c, _frames, 1);
            case CharacterStates.Cast: return new CastState(c, 60, 16, null);

        }

        return new DefaultState(c, _frames, 1);

    }

    public void SetState(CharacterStates _characterStates, int _animation, int _frames) {

        SetState(EnumToState(_characterStates, _animation, _frames));

    }

    public void InterpolateState(ushort _tick, CharacterStates _characterStates, int _animation, int _frames) {

        SetState(EnumToState(_characterStates, _animation, _frames));

        if (_tick < NetworkManager.instance.ServerTick) {

            int difference = NetworkManager.instance.ServerTick - _tick;
            SkipFrames(difference);

        }

    }

    private void SkipFrames(int _frames) {

        // TO DO: Skip animation

        if (curFrame < curState.ActionFrame) {

            if (curFrame + _frames > curState.ActionFrame) {

                //Debug.Log("SkipFrames -> Execute");
                curState.Execute();

            }

        }

        if (curFrame < curState.TotalFrames) {

            if (curFrame + _frames > curState.TotalFrames) {

                //Debug.Log("SkipFrames -> LastFrame");
                curState.LastFrame();

            }

        }

        // Do OnEveryFrame for each frame skipped?

        curFrame += _frames;

    }

    private void ClearBufferState() {

        if (bufferFrames > 0) {

            bufferFrames--;

            if (bufferFrames <= 0 && bufferState != null) {

                bufferState = null;

            }

        }

    }

}
