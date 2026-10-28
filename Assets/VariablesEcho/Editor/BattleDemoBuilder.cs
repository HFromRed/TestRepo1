using System;
using System.Collections.Generic;
using System.Linq;
using CCGKit;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VariablesEcho;

namespace VariablesEchoEditor
{
    /// <summary>
    /// 一键生成《变量残响》战斗演示：
    /// 卡牌、初始卡组、英雄、敌人、克制表、预制体、Addressables 条目与 BattleDemo 场景。
    /// 可重复执行，会重建 Demo/Data、Demo/Prefabs、Demo/Scenes，但不会动 Demo/Art。
    /// </summary>
    public static class BattleDemoBuilder
    {
        private const string Root = "Assets/VariablesEcho/Demo";
        private const string ArtRoot = Root + "/Art";
        private const string DataRoot = Root + "/Data";
        private const string PrefabRoot = Root + "/Prefabs";
        private const string SceneRoot = Root + "/Scenes";
        private const string ScenePath = SceneRoot + "/BattleDemo.unity";
        private const string SourceScenePath = "Assets/SinglePlayerCCGKit/Scenes/Game.unity";
        private const string EnemyDiedPath = "Assets/SinglePlayerCCGKit/GameData/Events/EnemyDied.asset";
        private const string AttackTypePath = "Assets/SinglePlayerCCGKit/GameData/Types/Attack.asset";
        private const string SkillTypePath = "Assets/SinglePlayerCCGKit/GameData/Types/Skill.asset";
        private const string WeakStatusPath = "Assets/SinglePlayerCCGKit/GameData/Status/Weak.asset";
        private const string StrengthStatusPath = "Assets/SinglePlayerCCGKit/GameData/Status/Strength.asset";

        private const string CardStrikePath = DataRoot + "/Cards/Strike.asset";
        private const string CardDefendPath = DataRoot + "/Cards/Defend.asset";
        private const string CardBashPath = DataRoot + "/Cards/Bash.asset";
        private const string CardNoiseChargePath = DataRoot + "/Cards/NoiseCharge.asset";
        private const string LibraryPath = DataRoot + "/Libraries/WardenStarterDeck.asset";
        private const string HeroPath = DataRoot + "/Heroes/Warden.asset";
        private const string EnemyPath = DataRoot + "/Enemies/NexusMarauder.asset";
        private const string ProfilePath = DataRoot + "/Enemies/NexusMarauderProfile.asset";
        private const string PlayerPrefabPath = PrefabRoot + "/Warden.prefab";
        private const string EnemyPrefabPath = PrefabRoot + "/NexusMarauder.prefab";

        [MenuItem("Variables Echo/Demo/Build Battle Demo")]
        public static void Build()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(DataRoot);
            EnsureFolder(PrefabRoot);
            EnsureFolder(SceneRoot);

            ImportArt();
            var sprites = LoadSprites();

            AssetDatabase.DeleteAsset(DataRoot);
            AssetDatabase.DeleteAsset(PrefabRoot);
            AssetDatabase.DeleteAsset(SceneRoot);
            AssetDatabase.Refresh();
            EnsureFolder(DataRoot);
            EnsureFolder(DataRoot + "/Cards");
            EnsureFolder(DataRoot + "/Libraries");
            EnsureFolder(DataRoot + "/Heroes");
            EnsureFolder(DataRoot + "/Enemies");
            EnsureFolder(PrefabRoot);
            EnsureFolder(SceneRoot);

            CleanDemoAddressables();

            var cardTypeAttack = AssetDatabase.LoadAssetAtPath<CardType>(AttackTypePath);
            var cardTypeSkill = AssetDatabase.LoadAssetAtPath<CardType>(SkillTypePath);
            var weak = AssetDatabase.LoadAssetAtPath<StatusTemplate>(WeakStatusPath);
            var strength = AssetDatabase.LoadAssetAtPath<StatusTemplate>(StrengthStatusPath);

            var cards = BuildCards(cardTypeAttack, cardTypeSkill, weak, sprites);
            var library = BuildLibrary(cards);
            var playerPrefab = BuildPlayerPrefab(sprites["warden"]);
            var hero = BuildHero(library, playerPrefab);
            var enemyPrefab = BuildEnemyPrefab(sprites);
            var enemy = BuildEnemy(enemyPrefab, sprites, strength, weak);

            EditorUtility.SetDirty(library);
            EditorUtility.SetDirty(hero);
            EditorUtility.SetDirty(enemy);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            MakeAddressable(HeroPath, "EchoDemo/WardenHero");
            MakeAddressable(EnemyPath, "EchoDemo/NexusMarauder");
            AssetDatabase.SaveAssets();

            BuildScene(hero, enemy, sprites["background"]);

            Debug.Log("[VariablesEcho] 战斗演示已生成：" + ScenePath);
            EditorUtility.DisplayDialog(
                "Variables Echo",
                "战斗演示生成完成。\n\n打开场景：\n" + ScenePath + "\n\n直接点击 Play 即可。",
                "好的");
        }

        // ------------------------------------------------------------------
        // 美术导入
        // ------------------------------------------------------------------

        private static void ImportArt()
        {
            ImportSprite(ArtRoot + "/Cards/strike.jpg", 100f, SpriteAlignment.Center);
            ImportSprite(ArtRoot + "/Cards/defend.png", 100f, SpriteAlignment.Center);
            ImportSprite(ArtRoot + "/Cards/bash.png", 100f, SpriteAlignment.Center);
            ImportSprite(ArtRoot + "/Cards/noise_charge.jpg", 100f, SpriteAlignment.Center);
            ImportSprite(ArtRoot + "/Characters/warden.png", 300f, SpriteAlignment.BottomCenter);
            ImportSprite(ArtRoot + "/Enemies/enemy_nexus_marauder_idle.png", 100f, SpriteAlignment.BottomCenter);
            ImportSprite(ArtRoot + "/Enemies/enemy_nexus_marauder_hit_01.png", 100f, SpriteAlignment.BottomCenter);
            ImportSprite(ArtRoot + "/Enemies/enemy_nexus_marauder_hit_02.png", 100f, SpriteAlignment.BottomCenter);
            ImportSprite(ArtRoot + "/Backgrounds/nexus_arena.png", 100f, SpriteAlignment.Center);

            // 意图图标会被 IntentWidget.SetNativeSize() 直接当成 UI 尺寸，
            // 用高 PPU 把 ~421px 的图标缩到 ~80 屏幕单位，避免遮挡敌人。
            foreach (var icon in new[] { "attack", "block", "noise", "lock" })
                ImportSprite(ArtRoot + "/UI/IntentIcons/" + icon + ".png", 512f, SpriteAlignment.Center);
        }

        private static Sprite ImportSprite(string path, float pixelsPerUnit, SpriteAlignment alignment)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException("找不到贴图：" + path);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)alignment;
            settings.spritePixelsPerUnit = pixelsPerUnit;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Dictionary<string, Sprite> LoadSprites()
        {
            return new Dictionary<string, Sprite>
            {
                { "strike", LoadSprite(ArtRoot + "/Cards/strike.jpg") },
                { "defend", LoadSprite(ArtRoot + "/Cards/defend.png") },
                { "bash", LoadSprite(ArtRoot + "/Cards/bash.png") },
                { "noise", LoadSprite(ArtRoot + "/Cards/noise_charge.jpg") },
                { "warden", LoadSprite(ArtRoot + "/Characters/warden.png") },
                { "enemyIdle", LoadSprite(ArtRoot + "/Enemies/enemy_nexus_marauder_idle.png") },
                { "enemyHit1", LoadSprite(ArtRoot + "/Enemies/enemy_nexus_marauder_hit_01.png") },
                { "enemyHit2", LoadSprite(ArtRoot + "/Enemies/enemy_nexus_marauder_hit_02.png") },
                { "background", LoadSprite(ArtRoot + "/Backgrounds/nexus_arena.png") },
                { "iconAttack", LoadSprite(ArtRoot + "/UI/IntentIcons/attack.png") },
                { "iconBlock", LoadSprite(ArtRoot + "/UI/IntentIcons/block.png") },
                { "iconNoise", LoadSprite(ArtRoot + "/UI/IntentIcons/noise.png") },
                { "iconLock", LoadSprite(ArtRoot + "/UI/IntentIcons/lock.png") }
            };
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                throw new InvalidOperationException("贴图未导入为 Sprite：" + path);
            return sprite;
        }

        // ------------------------------------------------------------------
        // 卡牌与卡组
        // ------------------------------------------------------------------

        private static Dictionary<string, CardTemplate> BuildCards(
            CardType attackType,
            CardType skillType,
            StatusTemplate weak,
            Dictionary<string, Sprite> sprites)
        {
            var cards = new Dictionary<string, CardTemplate>();

            var strike = CreateCard(CardStrikePath, 101, "Strike", 1, attackType, sprites["strike"],
                EchoCardFaction.Logic, EchoCardRarity.Starter, 0, false);
            var strikeDamage = AddEffect<DealDamageEffect>(strike, "Strike_Damage");
            strikeDamage.Value = 6;
            strikeDamage.Target = EffectTargetType.TargetEnemy;
            cards["strike"] = strike;

            var defend = CreateCard(CardDefendPath, 102, "Defend", 1, skillType, sprites["defend"],
                EchoCardFaction.Logic, EchoCardRarity.Starter, 0, false);
            var defendShield = AddEffect<GainShieldEffect>(defend, "Defend_Shield");
            defendShield.Value = 5;
            defendShield.Target = EffectTargetType.Self;
            cards["defend"] = defend;

            var bash = CreateCard(CardBashPath, 103, "Bash", 2, attackType, sprites["bash"],
                EchoCardFaction.Logic, EchoCardRarity.Starter, 0, false);
            var bashDamage = AddEffect<DealDamageEffect>(bash, "Bash_Damage");
            bashDamage.Value = 8;
            bashDamage.Target = EffectTargetType.TargetEnemy;
            var bashWeak = AddEffect<ApplyStatusEffect>(bash, "Bash_Weak");
            bashWeak.Value = 2;
            bashWeak.Target = EffectTargetType.TargetEnemy;
            bashWeak.Status = weak;
            cards["bash"] = bash;

            var noise = CreateCard(CardNoiseChargePath, 104, "Noise Charge", 0, skillType, sprites["noise"],
                EchoCardFaction.Noise, EchoCardRarity.Starter, 25, false);
            var noiseMana = AddEffect<GainManaEffect>(noise, "NoiseCharge_Mana");
            noiseMana.Value = 2;
            noiseMana.Target = EffectTargetType.Self;
            var noiseGain = AddEffect<NoiseEffect>(noise, "NoiseCharge_Noise");
            noiseGain.Value = 0; // NoiseDelta already applies +25 once.
            noiseGain.Target = EffectTargetType.Self;
            cards["noise"] = noise;

            foreach (var card in cards.Values)
            {
                EditorUtility.SetDirty(card);
            }

            AssetDatabase.SaveAssets();
            return cards;
        }

        private static CardTemplate CreateCard(
            string path,
            int id,
            string name,
            int cost,
            CardType type,
            Sprite picture,
            EchoCardFaction faction,
            EchoCardRarity rarity,
            int noiseDelta,
            bool isSelfHarm)
        {
            var card = ScriptableObject.CreateInstance<CardTemplate>();
            card.Id = id;
            card.Name = name;
            card.Cost = cost;
            card.Type = type;
            card.Picture = picture;
            // CardObject.SetInfo 会把 picture.material 设为 Template.Material；
            // 留空（null）会导致卡面图完全不渲染，必须给内置 Sprites-Default。
            card.Material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            card.CardId = name.Replace(" ", string.Empty).ToLowerInvariant();
            card.Faction = faction;
            card.Rarity = rarity;
            card.NoiseDelta = noiseDelta;
            card.IsSelfHarm = isSelfHarm;
            card.IsPermanent = false;
            AssetDatabase.CreateAsset(card, path);
            return card;
        }

        private static T AddEffect<T>(CardTemplate card, string assetName) where T : Effect
        {
            var effect = ScriptableObject.CreateInstance<T>();
            effect.name = assetName;
            AssetDatabase.AddObjectToAsset(effect, card);
            card.Effects.Add(effect);
            return effect;
        }

        private static CardLibrary BuildLibrary(Dictionary<string, CardTemplate> cards)
        {
            var library = ScriptableObject.CreateInstance<CardLibrary>();
            library.Name = "Warden Starter Deck";
            library.Entries.Add(new CardLibraryEntry { Card = cards["strike"], NumCopies = 5 });
            library.Entries.Add(new CardLibraryEntry { Card = cards["defend"], NumCopies = 4 });
            library.Entries.Add(new CardLibraryEntry { Card = cards["bash"], NumCopies = 1 });
            library.Entries.Add(new CardLibraryEntry { Card = cards["noise"], NumCopies = 2 });
            AssetDatabase.CreateAsset(library, LibraryPath);
            return library;
        }

        // ------------------------------------------------------------------
        // 角色与预制体
        // ------------------------------------------------------------------

        private static GameObject BuildPlayerPrefab(Sprite warden)
        {
            var root = new GameObject("Warden");
            var playerLayer = LayerMask.NameToLayer("Player");
            root.layer = playerLayer >= 0 ? playerLayer : 0;

            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(4.0f, 6.07f);
            collider.offset = new Vector2(0f, 3.03f);
            root.AddComponent<CharacterObject>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = warden;
            // Kit 场景的背景在 Background 层、角色在 Characters 层；
            // 若不指定，默认 Default 层会渲染在背景之后，导致角色被背景盖住。
            renderer.sortingLayerName = "Characters";
            renderer.sortingOrder = 5;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildEnemyPrefab(Dictionary<string, Sprite> sprites)
        {
            var root = new GameObject("NexusMarauder");
            var enemyLayer = LayerMask.NameToLayer("Enemy");
            root.layer = enemyLayer >= 0 ? enemyLayer : 0;

            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(5.54f, 6.99f);
            collider.offset = new Vector2(0f, 3.50f);

            var character = root.AddComponent<CharacterObject>();

            var listener = root.AddComponent<GameEventListener>();
            listener.Event = AssetDatabase.LoadAssetAtPath<GameEvent>(EnemyDiedPath);
            // 运行时通过 AddComponent 添加的 UnityEvent 字段可能为 null，先实例化再挂持久监听。
            if (listener.Response == null)
                listener.Response = new UnityEngine.Events.UnityEvent();
            UnityEventTools.AddVoidPersistentListener(listener.Response, character.OnCharacterDied);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.75f;
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites["enemyIdle"];
            renderer.sortingLayerName = "Characters";
            renderer.sortingOrder = 5;

            var flash = visual.AddComponent<EchoSpriteHitFlash>();
            flash.IdleSprite = sprites["enemyIdle"];
            flash.HitSprite1 = sprites["enemyHit1"];
            flash.HitSprite2 = sprites["enemyHit2"];

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static HeroTemplate BuildHero(CardLibrary library, GameObject playerPrefab)
        {
            var hero = ScriptableObject.CreateInstance<HeroTemplate>();
            hero.Name = "Warden";
            hero.Hp = 80;
            hero.Mana = 3;
            hero.Prefab = playerPrefab;
            hero.StartingDeck = library;
            hero.RewardDeck = library;
            AssetDatabase.CreateAsset(hero, HeroPath);
            return hero;
        }

        // ------------------------------------------------------------------
        // 敌人、Patterns 与克制表
        // ------------------------------------------------------------------

        private static EnemyTemplate BuildEnemy(
            GameObject enemyPrefab,
            Dictionary<string, Sprite> sprites,
            StatusTemplate strength,
            StatusTemplate weak)
        {
            var enemy = ScriptableObject.CreateInstance<EnemyTemplate>();
            enemy.Name = "Nexus Marauder";
            enemy.HpLow = 82;
            enemy.HpHigh = 88;
            enemy.Prefab = enemyPrefab;
            AssetDatabase.CreateAsset(enemy, EnemyPath);

            // 普通 Pattern 效果
            var attack9 = AddEnemyEffect<DealDamageEffect>(enemy, "Enemy_Attack9");
            attack9.Value = 9;
            attack9.Target = EffectTargetType.TargetEnemy;

            var shield6 = AddEnemyEffect<GainShieldEffect>(enemy, "Enemy_Shield6");
            shield6.Value = 6;
            shield6.Target = EffectTargetType.Self;

            var noise5 = AddEnemyEffect<NoiseEffect>(enemy, "Enemy_Noise5");
            noise5.Value = 5;
            noise5.Target = EffectTargetType.Self;

            // 普通循环：重击（1 次）-> 静电护盾（2 次）-> 重击 ...
            // 用 RepeatPattern（Times=1）而不是 RepeatForever，否则敌人会永远停在第一个意图上。
            var cageRush = ScriptableObject.CreateInstance<RepeatPattern>();
            cageRush.name = "Pattern_CageRush";
            cageRush.Times = 1;
            cageRush.Sprite = sprites["iconAttack"];
            cageRush.Effects.Add(attack9);
            AssetDatabase.AddObjectToAsset(cageRush, enemy);
            enemy.Patterns.Add(cageRush);

            var staticField = ScriptableObject.CreateInstance<RepeatPattern>();
            staticField.name = "Pattern_StaticField";
            staticField.Times = 2;
            staticField.Sprite = sprites["iconBlock"];
            staticField.Effects.Add(shield6);
            staticField.Effects.Add(noise5);
            AssetDatabase.AddObjectToAsset(staticField, enemy);
            enemy.Patterns.Add(staticField);

            // 腐蚀抓握：低伤害 + 给玩家上 1 层虚弱（受伤 25% 提升的反面：玩家输出下降），
            // 让普通循环里也出现 debuff，用来演示状态图标与悬停说明。
            var corrosiveDamage = AddEnemyEffect<DealDamageEffect>(enemy, "Enemy_CorrosiveDamage");
            corrosiveDamage.Value = 4;
            corrosiveDamage.Target = EffectTargetType.TargetEnemy;
            var corrosiveWeak = AddEnemyEffect<ApplyStatusEffect>(enemy, "Enemy_CorrosiveWeak");
            corrosiveWeak.Value = 1;
            corrosiveWeak.Target = EffectTargetType.TargetEnemy;
            corrosiveWeak.Status = weak;

            var corrosiveGrip = ScriptableObject.CreateInstance<RepeatPattern>();
            corrosiveGrip.name = "Pattern_CorrosiveGrip";
            corrosiveGrip.Times = 1;
            corrosiveGrip.Sprite = sprites["iconNoise"];
            corrosiveGrip.Effects.Add(corrosiveDamage);
            corrosiveGrip.Effects.Add(corrosiveWeak);
            AssetDatabase.AddObjectToAsset(corrosiveGrip, enemy);
            enemy.Patterns.Add(corrosiveGrip);

            // 克制表
            var profile = ScriptableObject.CreateInstance<EchoEnemyProfile>();
            profile.name = "NexusMarauderProfile";
            profile.faction = EchoAiFaction.NEXUS;
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var breakerDamage = AddEnemyEffect<DealDamageEffect>(enemy, "Counter_BreakerDamage");
            breakerDamage.Value = 5;
            breakerDamage.Target = EffectTargetType.TargetEnemy;
            var breakerNoise = AddEnemyEffect<NoiseEffect>(enemy, "Counter_BreakerNoise");
            breakerNoise.Value = 10;
            breakerNoise.Target = EffectTargetType.Self;
            profile.counters.Add(new EchoCounterEntry
            {
                tag = EchoBehaviorTag.Wall,
                label = "Breaker Howl",
                icon = sprites["iconNoise"],
                extraPredictLocks = 1,
                effects = new List<Effect> { breakerDamage, breakerNoise }
            });

            var bulwarkShield = AddEnemyEffect<GainShieldEffect>(enemy, "Counter_BulwarkShield");
            bulwarkShield.Value = 10;
            bulwarkShield.Target = EffectTargetType.Self;
            var bulwarkStrength = AddEnemyEffect<ApplyStatusEffect>(enemy, "Counter_BulwarkStrength");
            bulwarkStrength.Value = 2;
            bulwarkStrength.Target = EffectTargetType.Self;
            bulwarkStrength.Status = strength;
            profile.counters.Add(new EchoCounterEntry
            {
                tag = EchoBehaviorTag.AllIn,
                label = "Bulwark Protocol",
                icon = sprites["iconBlock"],
                extraPredictLocks = 1,
                effects = new List<Effect> { bulwarkShield, bulwarkStrength }
            });

            var flushNoise = AddEnemyEffect<NoiseEffect>(enemy, "Counter_FlushNoise");
            flushNoise.Value = 8;
            flushNoise.Target = EffectTargetType.Self;
            var flushDamage = AddEnemyEffect<DealDamageEffect>(enemy, "Counter_FlushDamage");
            flushDamage.Value = 4;
            flushDamage.Target = EffectTargetType.TargetEnemy;
            profile.counters.Add(new EchoCounterEntry
            {
                tag = EchoBehaviorTag.Draw,
                label = "Static Flush",
                icon = sprites["iconLock"],
                extraPredictLocks = 1,
                effects = new List<Effect> { flushNoise, flushDamage }
            });

            var mirrorReflect = AddEnemyEffect<ReflectEffect>(enemy, "Counter_MirrorReflect");
            mirrorReflect.Value = 3;
            mirrorReflect.Target = EffectTargetType.Self;
            var mirrorShield = AddEnemyEffect<GainShieldEffect>(enemy, "Counter_MirrorShield");
            mirrorShield.Value = 5;
            mirrorShield.Target = EffectTargetType.Self;
            profile.counters.Add(new EchoCounterEntry
            {
                tag = EchoBehaviorTag.SelfHarm,
                label = "Mirror Skin",
                icon = sprites["iconLock"],
                extraPredictLocks = 1,
                effects = new List<Effect> { mirrorReflect, mirrorShield }
            });

            var idleDamage = AddEnemyEffect<DealDamageEffect>(enemy, "Counter_IdleDamage");
            idleDamage.Value = 7;
            idleDamage.Target = EffectTargetType.TargetEnemy;
            profile.counters.Add(new EchoCounterEntry
            {
                tag = EchoBehaviorTag.Idle,
                label = "Idle Punish",
                icon = sprites["iconAttack"],
                extraPredictLocks = 1,
                effects = new List<Effect> { idleDamage }
            });

            EditorUtility.SetDirty(profile);
            enemy.EchoProfile = profile;
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        private static T AddEnemyEffect<T>(EnemyTemplate enemy, string assetName) where T : Effect
        {
            var effect = ScriptableObject.CreateInstance<T>();
            effect.name = assetName;
            AssetDatabase.AddObjectToAsset(effect, enemy);
            return effect;
        }

        // ------------------------------------------------------------------
        // 场景
        // ------------------------------------------------------------------

        private static void BuildScene(HeroTemplate hero, EnemyTemplate enemy, Sprite background)
        {
            AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(SourceScenePath, ScenePath))
                throw new InvalidOperationException("复制战斗场景失败：" + SourceScenePath);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();
            if (bootstrap == null)
                throw new InvalidOperationException("场景中找不到 GameBootstrap。");

            var heroRef = new AssetReference(AssetDatabase.AssetPathToGUID(HeroPath));
            var field = typeof(GameBootstrap).GetField(
                "characterTemplate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(bootstrap, heroRef);
            EditorUtility.SetDirty(bootstrap);

            var infoObject = new GameObject("BattleDemoGameInfo");
            var info = infoObject.AddComponent<GameInfo>();
            info.Encounter = new EncounterListEntry
            {
                Name = "Nexus Marauder",
                Background = background,
                Enemies = new List<AssetReference>
                {
                    new AssetReference(AssetDatabase.AssetPathToGUID(EnemyPath))
                }
            };
            infoObject.AddComponent<EchoDemoReset>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(x => x.path != ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        // ------------------------------------------------------------------
        // 工具
        // ------------------------------------------------------------------

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = path.Substring(0, path.LastIndexOf('/'));
            var name = path.Substring(path.LastIndexOf('/') + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void CleanDemoAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || settings.DefaultGroup == null)
                return;

            var group = settings.DefaultGroup;
            var stale = group.entries.Where(x => x.address.StartsWith("EchoDemo/")).ToList();
            foreach (var entry in stale)
                settings.RemoveAssetEntry(entry.guid);
        }

        private static void MakeAddressable(string path, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new InvalidOperationException("Addressables 设置不存在。");

            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
        }
    }
}

