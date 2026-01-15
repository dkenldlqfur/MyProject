using Game.Core;
using Game.Data;

namespace Game.Battle
{
    [SkillLogic(SkillLogicType.CheckResource)]
    public class CheckResourceEffectLogic : CheckEffectLogic<CheckResourceEffectData>
    {
        protected override bool OnCheck(CheckResourceEffectData data, SkillContext context)
        {
            Character checkTarget = TargetType.Caster == data.targetType ? context.caster : context.target;
            if (null == checkTarget)
                return false;

            if (data.resourceType.HasFlag(ResourceType.HP))
                if (!CheckSingleResource(data, checkTarget, ResourceType.HP))
                    return false;

            if (data.resourceType.HasFlag(ResourceType.MP))
                if (!CheckSingleResource(data, checkTarget, ResourceType.MP))
                    return false;

            if (data.resourceType.HasFlag(ResourceType.SP))
                if (!CheckSingleResource(data, checkTarget, ResourceType.SP))
                    return false;

            return true;
        }

        bool CheckSingleResource(CheckResourceEffectData data, Character target, ResourceType type)
        {
            int curVal = GetCurrentValue(target.CurrentStats, type);
            int targetVal = data.value;

            if (CheckResourceEffectData.CheckMode.PercentOfMax == data.checkMode)
            {
                int maxVal = GetMaxValue(target.BaseStats, type);
                return data.compareType.Evaluate((float)curVal * maxVal * 0.001f, (float)data.value);
            }

            return data.compareType.Evaluate(curVal, targetVal);
        }

        int GetCurrentValue(CharacterStats stats, ResourceType type)
        {
            return type switch
            {
                ResourceType.HP => stats.hp,
                ResourceType.MP => stats.mp,
                ResourceType.SP => stats.sp,
                _ => 0
            };
        }

        int GetMaxValue(CharacterStats stats, ResourceType type)
        {
            return type switch
            {
                ResourceType.HP => stats.hp,
                ResourceType.MP => stats.mp,
                ResourceType.SP => stats.sp,
                _ => 0
            };
        }
    }
}
