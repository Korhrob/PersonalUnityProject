using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModularStatusEffects {
    public class StatusEffectInstance {

        public StatusEffect baseEffect { get; private set; }
        public DamageData damageData { get; private set; }

        public StatusEffectInstance(Character source, StatusEffect baseEffect) {

            this.baseEffect = baseEffect;

            if (baseEffect.ConstainsModule<ComponentDOT>(out StatusEffectModule module)) {

                damageData = new DamageData(source);

                if (module.isPercentage)
                    damageData.value = (int)((float)damageData.value * module.value);
                else
                    damageData.value = (int)module.value;

            }

        }

        public void OnApply(Character c) {

            foreach (StatusEffectModule module in baseEffect.modules) {

                module.component.OnApply(c, this, module);

            }

            if (baseEffect.visualEffectType == VisualEffectType.OnApply) {

                VFX v = PrefabManager.instance.InstantiatePrefabType(baseEffect.visualEffect);
                v.Initialize(new PositionData(c.transform.position, Vector3.one), null);

            }

        }

        public void OnExpire(Character c) {

            foreach (StatusEffectModule module in baseEffect.modules) {

                module.component.OnExprire(c, this, module);

            }

        }

        public void Tick(Character c) {

            foreach (StatusEffectModule module in baseEffect.modules) {

                module.component.OnTick(c, this, module);

            }

            if (baseEffect.visualEffectType == VisualEffectType.OnTick) {

                VFX v = PrefabManager.instance.InstantiatePrefabType(baseEffect.visualEffect);
                v.Initialize(new PositionData(c.transform.position, Vector3.one), null);

            }

        }

    }

}
