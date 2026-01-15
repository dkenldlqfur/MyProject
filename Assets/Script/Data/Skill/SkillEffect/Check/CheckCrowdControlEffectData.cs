using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "NewCheckCrowdControlEffectData", menuName = "GameData/SkillData/CheckEffectData/CrowdControl")]
    public class CheckCrowdControlEffectData : CheckEffectData
    {
        public override SkillLogicType LogicType => SkillLogicType.CheckCrowdControl;

        public CrowdControlType ccType; // 대상의 CC 상태 여부 확인
    }
}
