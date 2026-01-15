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

        public class BattleAction
        {
            public Character Caster { get; private set; }
            public SkillData SkillData { get; private set; }
            public List<Character> Targets { get; private set; }
            public HitResult IncomingHitResult { get; private set; }
            public AttackType IncomingAttackType { get; private set; }
            public RangeType IncomingRangeType { get; private set; }

            public BattleAction(Character caster, SkillData skillData, List<Character> targets, HitResult hitResult, AttackType attackType, RangeType rangeType)
            {
                Caster = caster;
                SkillData = skillData;
                Targets = targets;
                IncomingHitResult = hitResult;
                IncomingAttackType = attackType;
                IncomingRangeType = rangeType;
            }
        }

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

        // Action Queue
        Queue<BattleAction> actionQueue = new Queue<BattleAction>();
        bool isProcessingQueue = false;

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
            // 하위 매니저 초기화
            unitManager = new UnitManager();
            
            // TurnManager 연결: AI 턴 실행 로직 주입
            turnManager = new TurnManager(unitManager, ExecuteAutoTurn);

            // CombatManager 연결: 스킬 실행 로직 주입 (EnqueueAction으로 변경)
            combatManager = new CombatManager(unitManager, EnqueueAction);
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

            EnqueueAction(caster, skill, targets);
            // 큐 처리 완료 후 EndTurn이 호출됨
        }

        /// <summary>
        /// AI 캐릭터의 자동 턴 실행 로직입니다.
        /// </summary>
        void ExecuteAutoTurn(Character character)
        {
            if (null == character)
                return;

            // 스킬 선택 로직 (임시: 랜덤 선택)
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
                // 타겟 선택 로직 (임시: 랜덤 선택)
                var targets = !character.isPlayerFaction ? unitManager.GetCharacters(true) : unitManager.GetCharacters(false);
                if (0 < targets.Count)
                {
                    var targetList = new List<Character>{ targets[UnityEngine.Random.Range(0, targets.Count)] };
                    EnqueueAction(character, selectedSkill, targetList);
                }
                else
                {
                   // 타겟 없음, 턴 스킵
                   turnManager.EndTurn();
                }
            }
            else
            {
                // 사용 가능 스킬 없음, 턴 스킵
                turnManager.EndTurn();
            }
        }

        /// <summary>
        /// 액션을 큐에 추가하고, 프로세서가 돌고 있지 않다면 시작합니다.
        /// </summary>
        public void EnqueueAction(Character caster, SkillData skillData, List<Character> targets, HitResult incomingHitResult = HitResult.None, AttackType incomingAttackType = AttackType.None, RangeType incomingRangeType = RangeType.None)
        {
            var action = new BattleAction(caster, skillData, targets, incomingHitResult, incomingAttackType, incomingRangeType);
            actionQueue.Enqueue(action);

            if (!isProcessingQueue)
                ProcessActionQueue().Forget();
        }

        async UniTaskVoid ProcessActionQueue()
        {
            isProcessingQueue = true;
            
            // 큐가 빌 때까지 계속 실행 (하나 끝나면 다음거)
            while (actionQueue.Count > 0)
            {
                var action = actionQueue.Dequeue();
                
                if (action.Caster.CurrentStats.hp <= 0)
                    continue;

                // 인터럽트 상태(피격/침묵 등)라면 스킬 취소 (스킬 체인 끊기)
                if (action.Caster.IsInterruptRequested)
                {
                    Debug.Log($"[Battle] {action.Caster.name}의 행동이 인터럽트되어 취소되었습니다.");
                    action.Caster.IsInterruptRequested = false; // 플래그 초기화
                    continue;
                }

                await ExecuteSkillAsync(action.Caster, action.SkillData, action.Targets, action.IncomingHitResult, action.IncomingAttackType, action.IncomingRangeType);
            }

            isProcessingQueue = false;
            
            // 모든 액션 시퀀스가 끝났으므로 턴 종료
            turnManager.EndTurn(); 
        }

        // ExecuteSkill을 외부에서 직접 호출하지 못하도록 막거나, EnqueueAction을 사용하도록 유도
        // 기존 ExecuteSkill은 제거하거나 Private으로 변경
        private async UniTask ExecuteSkillAsync(Character caster, SkillData skillData, List<Character> targets, HitResult incomingHitResult, AttackType incomingAttackType, RangeType incomingRangeType)
        {
            var hitInfos = new List<HitInfo>();
            UniTask attackEndTask = UniTask.CompletedTask;
            
            // 0. 애니메이터 오버라이드 설정 (존재하는 경우)
            if (SkillType.Active == skillData.SkillType)
            {
                foreach (var effect in skillData.effects)
                {
                    if (effect.animatorOverride != null)
                    {
                        caster.SetAnimatorOverride(effect.animatorOverride);
                        break;
                    }
                }
            }

            // 1. 타겟 이동 및 공격 (액티브 스킬 전용)
            if (SkillType.Active == skillData.SkillType)
            {
                // A. 이동
                // 타겟과의 거리가 충분히 가까우면(이미 이동해 있다면) 이동 애니메이션 생략
                bool skipMove = false;
                if (targets.Count > 0 && targets[0] != null)
                {
                    float dist = Vector3.Distance(caster.transform.position, targets[0].transform.position);
                    if (dist < 2.0f) // 임의의 근접 거리 임계값
                        skipMove = true;
                }

                if (!skipMove)
                {
                    caster.PlayAnimation("MoveToTarget");
                    
                    float moveDuration = caster.GetAnimationClipLength("Action_MoveToTarget");
                    if (0 >= moveDuration)
                        moveDuration = 1.0f; 
                    await UniTask.Delay((int)(moveDuration * 1000));
                }

                // B. 공격 (텍스트 표시)
                caster.PlayAnimation("Attack");
                
                float attackDuration = caster.GetAnimationClipLength("Action_Attack");
                if (0 >= attackDuration)
                    attackDuration = 0.5f;

                // 애니메이션 전체 길이에 대한 백그라운드 대기 (return 직전까지 동작 유지용)
                attackEndTask = UniTask.Delay((int)(attackDuration * 1000));

                // 타격 시점("Hit" 키)까지 대기
                // 애니메이션 길이만큼 Timeout을 설정하여 키가 없거나 놓쳤을 경우를 대비
                await WaitForAnimationKey(caster, "Hit", attackDuration + 0.1f);

                // C. 반응형 스킬 처리 (공격 전)
                // (타격 직전에 PreAttack을 수행하는 것이 논리적으로 적절)
                if (0 < targets.Count)
                    combatManager.ProcessReactionSkills(targets[0], caster, ReactionTiming.PreAttack);
            }

            foreach (var target in targets)
            {
                var context = new SkillContext(caster, target);
                
                // 공격 타입 초기화
                if (incomingAttackType != AttackType.None)
                    context.LastAttackType = incomingAttackType;
                
                // 사거리 타입 초기화
                RangeType currentRangeType = incomingRangeType;
                if (RangeType.None == currentRangeType)
                    currentRangeType = skillData.rangeType;
                context.LastRangeType = currentRangeType;

                // 피격 결과 초기화 (반응형 스킬용)
                if (HitResult.None != incomingHitResult)
                    context.LastHitResult = incomingHitResult;

                foreach (var effect in skillData.effects)
                    {
                        if (null == effect)
                            continue;
 
                    if (SkillEffectType.Conditional == effect.EffectType && !context.IsConditionCheck)
                        continue;

                    // 애니메이션 재생
                    if (!string.IsNullOrEmpty(effect.animationTriggerName))
                        caster.PlayAnimation(effect.animationTriggerName);

                    // 애니메이션 이벤트 대기
                    if (!string.IsNullOrEmpty(effect.triggerKey))
                        await WaitForAnimationKey(caster, effect.triggerKey);

                    SkillProcessor.Process(effect, context);

                    if (caster.IsInterruptRequested || context.AbortProcessing)
                    {
                        context.AbortProcessing = true; 
                        break;
                    }
                }

                // 피격 결과 수집
                if (HitResult.None != context.LastHitResult)
                    hitInfos.Add(new HitInfo(target, caster, context.LastHitResult));
                    
                // 회피 애니메이션 재생
                if (HitResult.Miss == context.LastHitResult)
                    target.PlayDodgeAnimation();
            }

            // 남은 애니메이션 시간 대기 (타격 후 동작 자연스럽게 연결)
            await attackEndTask;

            // 3. 속도 기준 정렬 (내림차순)
            hitInfos.Sort((a, b) => b.target.CurrentStats.speed.CompareTo(a.target.CurrentStats.speed));

            // 4. 후처리 트리거 (액티브 스킬 전용)
            if (SkillType.Active == skillData.SkillType)
            {
                // 피격 후 처리 (타겟)
                foreach (var info in hitInfos)
                {
                    combatManager.ProcessReactionSkills(info.target, info.attacker, ReactionTiming.PostHit, info.hitResult);
                }

                // 공격 후 처리 (시전)
                combatManager.ProcessReactionSkills(caster, null, ReactionTiming.PostAttack);
            }

            // 5. 사망 처리
            foreach (var info in hitInfos)
            {
                if (0 >= info.target.CurrentStats.hp)
                    ProcessDeath(info.target);
            }

            // 6. 복귀 애니메이션 & 상태 복원
            // 큐에 남은 액션이 있다면(연계된 반응 스킬 등) 복귀하지 않고 현 위치 유지
            if (SkillType.Active == skillData.SkillType)
            {
                if (actionQueue.Count == 0)
                {
                    caster.PlayAnimation("ReturnToStart");
                    
                    float returnDuration = caster.GetAnimationClipLength("Action_ReturnToStart");
                    if (0 >= returnDuration)
                        returnDuration = 1.0f;
                    await UniTask.Delay((int)(returnDuration * 1000));
                    
                    caster.RestoreAnimatorController();
                }
                else
                {
                    // 연계 동작이 남아있어 복귀 생략
                }
            }
        }

        /// <summary>
        /// 캐릭터 사망 처리를 수행합니다.
        /// </summary>
        void ProcessDeath(Character character)
        {
            // TODO: Implement death event, animation, and unit removal
            Debug.Log($"{character.name}가 사망했습니다.");
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
                if (targetKey == key)
                    triggered = true;
            };

            character.OnAnimationTriggered += handler;

            try
            {
                await UniTask.WaitUntil(() => triggered).Timeout(TimeSpan.FromSeconds(timeout));
            }
            catch (TimeoutException)
            {
                Debug.LogWarning($"애니메이션 키 '{targetKey}'가 {character.name}에 대해 시간 초과되었습니다.");
            }
            finally
            {
                character.OnAnimationTriggered -= handler;
            }
        }
    }
}