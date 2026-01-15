using Game.Core;
using Game.Data;

namespace Game.Battle
{
    [SkillLogic(SkillLogicType.ConditionalSetResource)]
    public class ConditionalSetResourceEffectLogic : ConditionalEffectLogic<ConditionalSetResourceEffectData>
    {
        protected override void OnExecute(ConditionalSetResourceEffectData data, SkillContext context)
        {
            Character target = TargetType.Caster == data.targetType ? context.caster : context.target;
            if (null == target)
                return;

            ApplySetResource(data, target);
        }

        void ApplySetResource(ConditionalSetResourceEffectData data, Character target)
        {
            if (data.resourceType.HasFlag(ResourceType.HP))
                ProcessSingleResource(target, ResourceType.HP, data);

            if (data.resourceType.HasFlag(ResourceType.MP))
                ProcessSingleResource(target, ResourceType.MP, data);

            if (data.resourceType.HasFlag(ResourceType.SP))
                ProcessSingleResource(target, ResourceType.SP, data);
        }

        void ProcessSingleResource(Character target, ResourceType type, ConditionalSetResourceEffectData data)
        {
            int currentVal = GetCurrentValue(target.CurrentStats, type);
            int diff = 0;

            switch (data.setMode)
            {
                case ConditionalSetResourceEffectData.SetMode.FixedValue:
                    diff = data.value - currentVal;
                    break;

                case ConditionalSetResourceEffectData.SetMode.PercentOfCurrent:
                    int reduction = (int)(currentVal * data.value * 0.01f);
                    diff = -reduction;
                    break;
            }

            if (diff != 0)
                target.RestoreResource(type, diff, false);
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
    }
}