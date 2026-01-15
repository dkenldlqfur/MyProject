using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewConditionalSetResourceEffectData", menuName = "GameData/SkillData/ConditionalEffectData/SetResource")]
    public class ConditionalSetResourceEffectData : ConditionalEffectData
    {
        public enum SetMode
        {
            FixedValue,     // 강제로 특정 값으로 설정
            PercentOfCurrent // 현재 자원의 %만큼 감소
        }

        public override SkillLogicType LogicType => SkillLogicType.ConditionalSetResource;        

        public ResourceType resourceType;
        public TargetType targetType = TargetType.Caster;
        public SetMode setMode = SetMode.FixedValue;
        public int value;
    }
}