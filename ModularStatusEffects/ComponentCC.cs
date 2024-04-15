using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModularStatusEffects {

    [CreateAssetMenu(fileName = "CrowdControl", menuName = "ModularStatusEffect/StatusEffectComponent/CrowdControl")]
    public class ComponentCC : StatusEffectComponent {

        // ComponentCC itself doesnt do anything, StatusEffectManager counts how many CC effects of each type are active
        // If anything, OnApply could inflict a specific CharacterState

        public StatusCCType statusControlType;

        public override void OnApply(Character c, StatusEffectInstance instance, StatusEffectModule module) {

        }

        public override void OnExprire(Character c, StatusEffectInstance instance, StatusEffectModule module) {

        }

        public override void OnTick(Character c, StatusEffectInstance instance, StatusEffectModule module) {

        }

    }

}