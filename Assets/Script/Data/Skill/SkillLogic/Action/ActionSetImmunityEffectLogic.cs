using Game.Core;
using Game.Data;
using UnityEngine;

namespace Game.Battle
{
    [SkillLogic(SkillLogicType.ActionSetImmunity)]
    public class ActionSetImmunityEffectLogic : ActionEffectLogic<ActionSetImmunityEffectData>
    {
        protected override void OnExecute(ActionSetImmunityEffectData data, SkillContext context)
        {
            // 자신에게 면역 상태 부여
            // context.caster가 면역을 받는 주체 (Buff self)
            // 타겟에게 부여하려면 context.target 사용 (스킬 설계에 따라 다름, 보통 Buff는 Self or Ally)
            
            // 여기서는 context.target에게 부여하는 것으로 가정 (Buff Skill이 아군을 타겟팅했다면 target이 아군)
            // 만약 Self Buff라면 caster == target 일 것임.
            if (null != context.target)
            {
                context.target.AddImmunity(data.immunityAttackType, data.immunityRangeType);
                Debug.Log($"[Immunity] {context.target.name}에게 면역 적용됨 : {data.immunityAttackType} + {data.immunityRangeType}");
            }
        }
    }
}
