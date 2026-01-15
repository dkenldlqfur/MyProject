using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewCheckResourceEffectData", menuName = "GameData/SkillData/CheckEffectData/Resource")]
    public class CheckResourceEffectData : CheckEffectData
    {
        public enum CheckMode
        {
            FixedValue,     // 고정 값 비교
            PercentOfMax,   // % 비율 비교
        };

        public override SkillLogicType LogicType => SkillLogicType.CheckResource;

        public ResourceType resourceType = ResourceType.HP;
        public TargetType targetType = TargetType.Caster;
        public CheckMode checkMode = CheckMode.FixedValue;
        public CompareType compareType = CompareType.Equal;

        public int value = 0;
    }
}
