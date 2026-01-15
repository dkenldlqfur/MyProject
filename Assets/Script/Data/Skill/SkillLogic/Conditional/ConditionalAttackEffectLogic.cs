using Game.Core;
using Game.Data;
using UnityEngine;

namespace Game.Battle
{
    [SkillLogic(SkillLogicType.ConditionalAttack)]
    public class ConditionalAttackEffectLogic : ConditionalEffectLogic<ConditionalAttackEffectData>
    {
        protected override void OnExecute(ConditionalAttackEffectData data, SkillContext context)
        {
            // 1. 기본 데미지(Raw Damage) 계산
            int rawPhys = (int)(context.caster.CurrentStats.physAttack * data.physPowerPercent * 0.01f);
            int rawMagic = (int)(context.caster.CurrentStats.magicAttack * data.magicPowerPercent * 0.01f);

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

            context.LastHitResult = context.target.TakeDamage(context.caster, rawPhys, rawMagic, data, type, rangeType);
        }
    }
}
