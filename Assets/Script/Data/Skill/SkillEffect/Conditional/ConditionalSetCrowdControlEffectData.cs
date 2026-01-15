using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewConditionalSetCrowdControlEffectData", menuName = "GameData/SkillData/ConditionalEffectData/SetCrowdControl")]
    public class ConditionalSetCrowdControlEffectData : ConditionalEffectData
    {
        public override SkillLogicType LogicType => SkillLogicType.ConditionalSetCrowdControl;

        public CrowdControlType ccType = CrowdControlType.None; // 상태 이상 부여
    }
}
