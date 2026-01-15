using Game.Core;
using Game.Data;

namespace Game.Battle
{
    [SkillLogic(SkillLogicType.ActionResourceRestore)]
    public class ActionResourceRestoreEffectLogic : ActionEffectLogic<ActionResourceRestoreEffectData>
    {
        protected override void OnExecute(ActionResourceRestoreEffectData data, SkillContext context)
        {
            Character target = TargetType.Caster == data.targetType ? context.caster : context.target;
            if (null == target)
                return;

            ApplyRestore(data, target);
        }

        void ApplyRestore(ActionResourceRestoreEffectData data, Character target)
        {
            if (data.resourceType.HasFlag(ResourceType.HP))
                ProcessSingleResource(target, ResourceType.HP, data);

            if (data.resourceType.HasFlag(ResourceType.MP))
                ProcessSingleResource(target, ResourceType.MP, data);

            if (data.resourceType.HasFlag(ResourceType.SP))
                ProcessSingleResource(target, ResourceType.SP, data);
        }

        void ProcessSingleResource(Character target, ResourceType type, ActionResourceRestoreEffectData data)
        {
            int restoreValue = 0;
            switch (data.restoreMode)
            {
                case ActionResourceRestoreEffectData.RestoreMode.FixedValue:
                    restoreValue = data.value;
                    break;

                case ActionResourceRestoreEffectData.RestoreMode.PercentOfMax:
                    int maxValue = GetMaxValue(target.BaseStats, type);
                    restoreValue = (int)(maxValue * data.value * 0.01f);
                    break;
            }

            target.RestoreResource(type, restoreValue, data.isOverHeal);
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
