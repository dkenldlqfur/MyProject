using Game.Core;
using Game.Data;

namespace Game.Battle
{
    [SkillLogic(SkillLogicType.ConditionalSetCrowdControl)]
    public class ConditionalSetCrowdControlEffectLogic : ConditionalEffectLogic<ConditionalSetCrowdControlEffectData>
    {
        protected override void OnExecute(ConditionalSetCrowdControlEffectData data, SkillContext context)
        {
            context.target.SetCrowdControlState(data.ccType, context.caster);
        }
    }
}

