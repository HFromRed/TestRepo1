using CCGKit;
using UnityEngine;

namespace VariablesEcho
{
    /// <summary>
    /// 演示用敌人受击动画：监听自身 HP 变化，短暂切换到受击帧。
    /// 不依赖 Animator，方便直接使用素材库里的 idle/hit 序列帧。
    /// </summary>
    public class EchoSpriteHitFlash : MonoBehaviour
    {
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite hitSprite1;
        [SerializeField] private Sprite hitSprite2;
        [SerializeField] private float hitDuration = 0.16f;

        private SpriteRenderer spriteRenderer;
        private CharacterObject character;
        private int lastHp = -1;
        private float hitTimer;

        public Sprite IdleSprite { get { return idleSprite; } set { idleSprite = value; } }
        public Sprite HitSprite1 { get { return hitSprite1; } set { hitSprite1 = value; } }
        public Sprite HitSprite2 { get { return hitSprite2; } set { hitSprite2 = value; } }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            character = GetComponentInParent<CharacterObject>();
        }

        private void Update()
        {
            if (character == null || character.Character == null || spriteRenderer == null)
                return;

            var hp = character.Character.Hp.Value;

            if (lastHp < 0)
            {
                lastHp = hp;
                return;
            }

            if (hp < lastHp)
            {
                lastHp = hp;
                hitTimer = hitDuration;
                if (hitSprite1 != null && hitSprite2 != null)
                    spriteRenderer.sprite = Random.value < 0.5f ? hitSprite1 : hitSprite2;
                else if (hitSprite1 != null)
                    spriteRenderer.sprite = hitSprite1;
            }

            if (hitTimer > 0f)
            {
                hitTimer -= Time.deltaTime;
                if (hitTimer <= 0f && idleSprite != null)
                    spriteRenderer.sprite = idleSprite;
            }
        }
    }
}
