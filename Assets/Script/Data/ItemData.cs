namespace Game.Data
{
    using UnityEngine;

    /// <summary>
    /// 아이템 데이터를 정의하는 클래스입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "GameData/ItemData")]
    public class ItemData : ScriptableObject
    {
        public string itemName; // 아이템 이름
        public CharacterStats stats; // 아이템 장착 시 보너스 스탯

        public SkillData skillData;
        public List<SkillEffect> providedEffects = new();
    }
}