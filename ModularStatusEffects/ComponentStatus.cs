using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModularStatusEffects {

    [CreateAssetMenu(fileName = "Status", menuName = "ModularStatusEffect/StatusEffectComponent/Status")]
    public class ComponentStatus : StatusEffectComponent {

        public StatusBonusType statusBonusType;
        public Stat.Type statusType;

        public override void OnApply(Character c, StatusEffectInstance instance, StatusEffectModule module) {

            if (statusBonusType == StatusBonusType.Penalty)
                module.value = -module.value;

            c.characterStatus.AddFinalStatusModifier(statusType, module.value);

        }

        public override void OnExprire(Character c, StatusEffectInstance instance, StatusEffectModule module) {

            if (statusBonusType == StatusBonusType.Penalty)
                module.value = -module.value;

            c.characterStatus.AddFinalStatusModifier(statusType, -module.value);

        }

        public override void OnTick(Character c, StatusEffectInstance instance, StatusEffectModule module) {

        }

    }

}