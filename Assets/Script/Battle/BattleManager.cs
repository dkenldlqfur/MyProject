using UnityEngine;
using Game.Core;
using Game.Data;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Game.Battle
{
    /// <summary>
    /// 전투의 전반적인 흐름과 로직을 관리하는 파사드(Facade) 클래스입니다.
    /// 실제 로직은 각 하위 매니저(UnitManager, TurnManager, CombatManager)로 위임합니다.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        struct HitInfo
        {
            public Character target;
            public Character attacker;
            public HitResult hitResult;

            public HitInfo(Character target, Character attacker, HitResult hitResult)
            {
                this.target = target;
                this.attacker = attacker;
                this.hitResult = hitResult;
            }
        }

        [Header("Transform References")]
        public Transform allyGridRoot;  // 아군 배치 루트
        public Transform enemyGridRoot; // 적군 배치 루트

        // Sub-Managers
        public UnitManager unitManager;
        public TurnManager turnManager;
        public CombatManager combatManager;

        // Legacy Support Properties
        public List<Character> AllyCharacters => unitManager.allyCharacters;
        public List<Character> EnemyCharacters => unitManager.enemyCharacters;
        public Character CurrentTurnCharacter => turnManager.currentTurnCharacter;
        public Queue<Character> TurnQueue => turnManager.turnQueue;

        void Awake()
        {
            if (null != Instance && this != Instance)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeSubManagers();
        }

        void InitializeSubManagers()
        {
            unitManager = new UnitManager();
            
            // 콜백 연결: TurnManager가 AI 턴을 진행하려 할 때 호출할 메서드 지정
            turnManager = new TurnManager(unitManager, ExecuteAutoTurn);

            // 콜백 연결: CombatManager가 스킬을 실행해야 할 때 호출할 메서드 지정
            combatManager = new CombatManager(unitManager, ExecuteSkill);
        }

        public void StartBattle()
        {
            turnManager.StartNextTurn();
        }

        /// <summary>
        /// 캐릭터를 전투 시스템에 등록합니다.
        /// </summary>
        public void RegisterCharacter(Character character, bool isAlly)
        {
            unitManager.RegisterCharacter(character, isAlly);
            
            // 이벤트 구독 (Decoupling)
            character.OnAttackRequested += ProcessAttack;
            character.OnRestoreRequested += ProcessRestoreResource;
            character.OnCrowdControlRequested += ProcessCrowdControlState;
        }

        public List<Character> GetAllCharacters()
        {
            return unitManager.GetAllCharacters();
        }

        /// <summary>
        /// 턴 관련 메서드들 (Facade Pattern)
        /// </summary>
        public void StartNextTurn()
        {
            turnManager.StartNextTurn();
        }

        public void EndTurn()
        {
            turnManager.EndTurn();
        }

        /// <summary>
        /// 외부(UI 등)에서 스킬 사용을 요청할 때 사용합니다.
        /// </summary>
        public void RequestUseSkill(Character caster, SkillData skill, List<Character> targets)
        {
            if (turnManager.currentTurnCharacter != caster)
                return;

            ExecuteSkill(caster, skill, targets);
            turnManager.EndTurn();
        }

        /// <summary>
        /// AI 캐릭터의 자동 턴 실행 로직입니다.
        /// </summary>
        void ExecuteAutoTurn(Character character)
        {
            if (null == character)
                return;

            // 스킬 선택 logic (임시: 랜덤)
            SkillData selectedSkill = null;
            foreach (var skill in character.equippedSkills)
            {
                if (null != skill && SkillType.Active == skill.skillType)
                {
                    selectedSkill = skill;
                    break;
                }
            }

            if (null != selectedSkill)
            {
                // 타겟 logic (임시: 랜덤)
                var targets = !character.isPlayerFaction ? unitManager.GetCharacters(true) : unitManager.GetCharacters(false);
                if (0 < targets.Count)
                {
                    var targetList = new List<Character>{ targets[UnityEngine.Random.Range(0, targets.Count)] };
                    ExecuteSkill(character, selectedSkill, targetList);
                }
            }

            turnManager.EndTurn();
        }

        /// <summary>
        /// 스킬을 실제로 실행하는 중앙 관리 메서드입니다.
        /// </summary>
        /// <param name="incomingHitResult">반응 스킬용 피격 결과 (일반 스킬은 기본값 사용)</param>
        public void ExecuteSkill(Character caster, SkillData skillData, List<Character> targets, HitResult incomingHitResult = HitResult.None, AttackType incomingAttackType = AttackType.None, RangeType incomingRangeType = RangeType.None)
        {
            ExecuteSkillAsync(caster, skillData, targets, incomingHitResult, incomingAttackType, incomingRangeType).Forget();
        }

        private async UniTaskVoid ExecuteSkillAsync(Character caster, SkillData skillData, List<Character> targets, HitResult incomingHitResult, AttackType incomingAttackType, RangeType incomingRangeType)
        {
            var hitInfos = new List<HitInfo>();

            // 1. PreAttack Trigger (Active Skill Only)
            if (SkillType.Active == skillData.SkillType)
            {
                // TODO: Handle AOE target selection for reaction skills properly (currently using first target)
                if (targets.Count > 0)
                    combatManager.ProcessReactionSkills(targets[0], caster, ReactionTiming.PreAttack);
            }

            // 2. Cooldown Management
            if (SkillType.Active == skillData.SkillType)
            {
                // TODO: Integrate with CooldownManager
            }

            foreach (var target in targets)
            {
                var context = new SkillContext(caster, target);
                
                // Initialize AttackType
                if (incomingAttackType != AttackType.None)
                    context.LastAttackType = incomingAttackType;
                
                // Initialize RangeType
                RangeType currentRangeType = incomingRangeType;
                if (currentRangeType == RangeType.None)
                    currentRangeType = skillData.rangeType;
                context.LastRangeType = currentRangeType;

                // Initialize HitResult (for Reaction Skills)
                if (HitResult.None != incomingHitResult)
                    context.LastHitResult = incomingHitResult;

                foreach (var effect in skillData.effects)
                    {
                        if (null == effect)
                            continue;
 
                    if (SkillEffectType.Conditional == effect.EffectType && !context.IsConditionCheck)
                        continue;

                    // Animation Play
                    if (!string.IsNullOrEmpty(effect.animationTriggerName))
                        caster.PlayAnimation(effect.animationTriggerName);

                    // Wait for Animation Event
                    if (!string.IsNullOrEmpty(effect.triggerKey))
                        await WaitForAnimationKey(caster, effect.triggerKey);

                    SkillProcessor.Process(effect, context);

                    if (caster.IsInterruptRequested || context.AbortProcessing)
                    {
                        context.AbortProcessing = true; 
                        break;
                    }
                }

                // Collect Hit Results
                if (HitResult.None != context.LastHitResult)
                    hitInfos.Add(new HitInfo(target, caster, context.LastHitResult));
            }

            // 3. Sort by Speed (Descending)
            hitInfos.Sort((a, b) => b.target.CurrentStats.speed.CompareTo(a.target.CurrentStats.speed));

            // 4. Post-Process Triggers (Active Skill Only)
            if (SkillType.Active == skillData.SkillType)
            {
                // Post-Hit (Target)
                foreach (var info in hitInfos)
                {
                    combatManager.ProcessReactionSkills(info.target, info.attacker, ReactionTiming.PostHit, info.hitResult);
                }

                // Post-Attack (Caster)
                combatManager.ProcessReactionSkills(caster, null, ReactionTiming.PostAttack);
            }

            // 5. Process Death
            foreach (var info in hitInfos)
            {
                if (0 >= info.target.CurrentStats.hp)
                    ProcessDeath(info.target);
            }
        }

        /// <summary>
        /// 캐릭터 사망 처리를 수행합니다.
        /// </summary>
        void ProcessDeath(Character character)
        {
            // TODO: Implement death event, animation, and unit removal
            Debug.Log($"{character.name} has died.");
        }

        /// <summary>
        /// 공격 처리 및 리소스 관리 메서드 (Facade -> CombatManager)
        /// </summary>
        public HitResult ProcessAttack(Character attacker, Character target, int physDamage, int magicDamage, SkillEffectData sourceEffect, AttackType attackType, RangeType rangeType)
        {
            return combatManager.ProcessAttack(attacker, target, physDamage, magicDamage, sourceEffect, attackType, rangeType);
        }

        public void ProcessRestoreResource(Character target, ResourceType resourceType, int value, bool isOverHeal)
        {
            combatManager.ProcessRestoreResource(target, resourceType, value, isOverHeal);
        }

        public void ProcessCrowdControlState(Character target, Character source, CrowdControlType ccType)
        {
            combatManager.ProcessCrowdControlState(target, source, ccType);
        }

        private async UniTask WaitForAnimationKey(Character character, string targetKey, float timeout = 10.0f)
        {
            if (string.IsNullOrEmpty(targetKey))
                return;

            bool triggered = false;
            Action<string> handler = (key) =>
            {
                if (key == targetKey)
                    triggered = true;
            };

            character.OnAnimationTriggered += handler;

            try
            {
                await UniTask.WaitUntil(() => triggered).Timeout(TimeSpan.FromSeconds(timeout));
            }
            catch (TimeoutException)
            {
                Debug.LogWarning($"Animation Key '{targetKey}' timed out for {character.name}.");
            }
            finally
            {
                character.OnAnimationTriggered -= handler;
            }
        }
    }
}