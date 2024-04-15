using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModularStatusEffects {

    public abstract class StatusEffectComponent : ScriptableObject
    {

        public abstract void OnApply(Character c, StatusEffectInstance instance, StatusEffectModule module);
        public abstract void OnExprire(Character c, StatusEffectInstance instance, StatusEffectModule module);
        public abstract void OnTick(Character c, StatusEffectInstance instance, StatusEffectModule module);

    }

    [System.Serializable]
    public class StatusEffectModule {

        public StatusEffectComponent component;

        // should value be treated as percentage
        // ComponentCC, percentage of statusEffect.baseDuration
        public bool isPercentage;

        // base value for component
        // ComponentCC, duration of cc (0 = statusEffect.baseDuration)
        // ComponentDOT, damage per tick
        // ComponentStatus, stat value
        public float value;


        // intervals this component activates Tick()
        // 0 = does not trigger Tick()
        public float interval;

    }

}