using Game.Core;
using Game.Data;

namespace Game.Battle
{
    [SkillLogic(SkillLogicType.CheckHitResult)]
    public class CheckHitResultEffectLogic : CheckEffectLogic<CheckHitResultEffectData>
    {
        protected override bool OnCheck(CheckHitResultEffectData data, SkillContext context)
        {
            // 1. HitResult 비교
            if (data.hitResult != context.LastHitResult)
                return false;

            // 2. AttackType 비교 (None이면 무조건 통과)
            if (AttackType.None != data.attackType)
            {
                // LastAttackType이 조건에 포함되는지 확인 (Flags)
                if ((context.LastAttackType & data.attackType) == 0)
                    return false;
            }

            // 3. RangeType 비교 (None이면 무조건 통과)
            if (RangeType.None != data.checkRangeType)
            {
                // LastRangeType이 조건과 일치하는지 확인 (Exact Match)
                if (data.checkRangeType != context.LastRangeType)
                    return false;
            }

            return true;
        }
    }
}