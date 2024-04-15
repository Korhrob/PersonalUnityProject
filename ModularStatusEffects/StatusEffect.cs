/* Modular Status Effects by Shiiiifttt (Robert Korhonen)
 * version 0.0.1
 * This is a modular status effect system build for Unity (tested in 2021.3.24f1)
 * using Scriptable Objects to make it easy to make and customize status effects
 * TODO:
 * StatusEffectManager
 * Make ComponentStatus type into flags
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModularStatusEffects {

    public enum StatusCCType {
        NoMove = 1 << 0,
        NoAttack = 1 << 1,
        NoCast = 1 << 2
    }

    public enum StatusBonusType {
        Bonus,
        Penalty
    }

    public enum ReApplyType {
        RefreshDuration,
        StackDuration,
        Stack,
        Immune,
    }

    public enum VisualEffectType {
        None,
        OnApply,
        OnTick
    }

    [CreateAssetMenu(fileName = "StatusEffect", menuName = "ModularStatusEffect/StatusEffect")]
    public class StatusEffect : ScriptableObject
    {

        [Header("Info")]
        public Sprite displayIcon;
        public string displayName;
        public float baseDuration;
        public ReApplyType reApplyType;

        [Header("Visual")]
        public VisualEffectType visualEffectType;
        public VFX visualEffect;
        public bool attachVFX;

        [Header("Audio")]
        public VisualEffectType soundEffectType;
        public AudioClip soundEffect;

        [Header("Components")]
        public StatusEffectModule[] modules;

        public bool ConstainsModule<T>(out StatusEffectModule component) where T : StatusEffectComponent {

            component = null;

            for (var i = 0; i < modules.Length; i++) {

                if (modules[i] is T) {
                    component = modules[i];
                    return true;
                }

            }

            return false;

        }

    }

}
