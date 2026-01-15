using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewActionResourceRestoreEffectData", menuName = "GameData/SkillData/ActionEffectData/ResourceRestore")]
    public class ActionResourceRestoreEffectData : ActionEffectData
    {
        public enum RestoreMode
        {
            FixedValue,
            PercentOfMax,
            ScalingValue
        }

        public override SkillLogicType LogicType => SkillLogicType.ActionResourceRestore;

        public ResourceType resourceType;
        public TargetType targetType = TargetType.Target; // Default to Target
        public RestoreMode restoreMode;
        public int value;

        public bool isOverHeal = false;
    }
}
