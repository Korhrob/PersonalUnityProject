using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModularStatusEffects {

    [CreateAssetMenu(fileName = "DamageOverTime", menuName = "ModularStatusEffect/StatusEffectComponent/DamageOverTime")]
    public class ComponentDOT : StatusEffectComponent {

        public override void OnApply(Character c, StatusEffectInstance instance, StatusEffectModule module) {

        }

        public override void OnExprire(Character c, StatusEffectInstance instance, StatusEffectModule module) {

        }

        public override void OnTick(Character c, StatusEffectInstance instance, StatusEffectModule module) {

            c.LocalTakeDamage(instance.damageData);

        }

    }

}