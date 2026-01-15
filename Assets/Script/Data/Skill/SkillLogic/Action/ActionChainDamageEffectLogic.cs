using Game.Core;
using Game.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle
{
    [SkillLogic(SkillLogicType.ActionChainDamage)]
    public class ActionChainDamageEffectLogic : ActionEffectLogic<ActionChainDamageEffectData>
    {
        protected override void OnExecute(ActionChainDamageEffectData data, SkillContext context)
        {
            if (null == context.target)
                return;

            for (int i = 0; i < data.maxTargets; ++i)
            {
                float multiplier = 1.0f;
                if (ActionChainDamageEffectData.ChainMode.Decay == data.chainMode)
                    multiplier = Mathf.Pow(data.decayRate * 0.01f, i);
                else if (ActionChainDamageEffectData.ChainMode.Distributed == data.chainMode && i < data.decayRates.Count)
                    multiplier = data.decayRates[i] * 0.0001f;

                // 1. 기본 데미지 계산 (체인 승수 적용)
                int rawPhys = (int)(context.caster.CurrentStats.physAttack * data.physPowerPercent * 0.01f * multiplier);
                int rawMagic = (int)(context.caster.CurrentStats.magicAttack * data.magicPowerPercent * 0.01f * multiplier);

                context.PhysDamage += rawPhys;
                context.MagicDamage += rawMagic;

                // 2. 공격 타입 설정
                AttackType type = context.LastAttackType;
                if (data.physPowerPercent > 0)
                    type |= AttackType.Physical;
                if (data.magicPowerPercent > 0)
                    type |= AttackType.Magic;

                // 3. 사거리 타입 설정
                RangeType rangeType = context.LastRangeType;

                if (0 == i)
                    context.LastHitResult = context.target.TakeDamage(context.caster, rawPhys, rawMagic, data, type, rangeType);

                // TODO: 다음 타겟 탐색 로직 구현 필요
            }
        }
    }
}
