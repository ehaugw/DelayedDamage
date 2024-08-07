using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DelayedDamage
{
    using TinyHelper;

    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(TinyHelper.GUID, TinyHelper.VERSION)]
    public class DelayedDamage : BaseUnityPlugin
    {

        public const string GUID = "com.ehaugw.delayeddamage";
        public const string VERSION = "2.1.0";
        public const string NAME = "Delayed Damage";

        public static DelayedDamage Instance;
        
        public StatusEffect DelayedDamageInstance;

        [Obsolete("GetDamageToDelay is deprecated because it would only track one function, please append to GetDamageToDelayList instead to track everything.")]
        public static Func<Character, Character, DamageList, float, float> GetDamageToDelay = delegate (Character character, Character dealer, DamageList damageList, float damage) { return 0; };
        public static List<Func<Character, Character, DamageList, float, float>> GetDamageToDelayList = new List<Func<Character, Character, DamageList, float, float>> { };
        public static Action<Character, Character, float> OnDelayedDamageTaken = delegate (Character character, Character dealer, float damage) { };

        internal void Awake()
        {
            Instance = this;
            var harmony = new Harmony(GUID);
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(ResourcesPrefabManager), "Load")]
        public class ResourcesPrefabManager_Load
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (ResourcesPrefabManager.Instance?.Loaded ?? false)
                {
                    //Instance.delayedDamageSources.Add(new TestDelayedDamageSource());
                    MakeDelayedDamageStatusEffect();
                }
            }
        }

        private static void MakeDelayedDamageStatusEffect()
        {
            DelayedDamage.Instance.DelayedDamageInstance = TinyEffectManager.MakeStatusEffectPrefab(
                effectName:         "Delayed Damage",
                familyName:         "Delayed Damage",
                description:        "A lingering effect that affects your health over time.",
                lifespan:           DelayedDamageEffect.LifeSpan,
                refreshRate:        DelayedDamageEffect.RefreshRate,
                stackBehavior:      StatusEffectFamily.StackBehaviors.Independant,
                targetStatusName:   "Bleeding",
                isMalusEffect:      true,
                modGUID:            GUID
            );

            var effectSignature = DelayedDamage.Instance.DelayedDamageInstance.StatusEffectSignature;
            var effectComponent = TinyGameObjectManager.MakeFreshObject("Effects", true, true, effectSignature.transform).AddComponent<DelayedDamageEffect>();
            effectComponent.UseOnce = false;
            effectSignature.Effects = new List<Effect>() { effectComponent };
        }

        [HarmonyPatch(typeof(Character), "OnReceiveHit", new Type[] { typeof(Weapon), typeof(float), typeof(DamageList), typeof(Vector3), typeof(Vector3), typeof(float), typeof(float), typeof(Character), typeof(float) })]
        public class Character_ReceiveHit
        {
            [HarmonyPrefix]
            public static void Prefix(Character __instance, ref float _damage, Character _dealerChar, DamageList _damageList, float _knockBack)
            {
                DelaySomeDamage(__instance, _dealerChar, ref _damage, _damageList, _knockBack);
            }
        }

        public static void DelaySomeDamage(Character character, Character dealer, ref float damage, DamageList damageList, float knockBack)
        {

            float delayedDamage = Mathf.Clamp(DelayedDamage.GetDamageToDelayList.Select(func => func(character, dealer, damageList, knockBack)).Sum(), 0, damage) + DelayedDamage.GetDamageToDelay(character, dealer, damageList, knockBack);
            if (delayedDamage > 0)
            {
                damage -= delayedDamage;
                DealDelayedDamage(character, dealer, delayedDamage);
            }
        }

        public static void DealDelayedDamage(Character character, Character dealer, float delayedDamage)
        {
            if (character?.StatusEffectMngr?.AddStatusEffect("Delayed Damage") is StatusEffect statusEffect)
            {
                if (dealer != null)
                {
                    statusEffect.SetSourceCharacter(dealer);
                }
                statusEffect.SetPotency(delayedDamage);
            }
        }
        
        public static float GetRemainingDelayedDamage(Character character)
        {
            return character?.StatusEffectMngr?.Statuses?.Where(status => status.IdentifierName == "Delayed Damage").Select(status => status.EffectPotency)?.Sum() ?? 0;
        }

    }

    public class DelayedDamageEffect : Effect
    {
        public static int LifeSpan = 15;
        public static float RefreshRate = 1;

        protected override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            float damage = -(this.m_parentStatusEffect?.EffectPotency ?? 1) / (LifeSpan / RefreshRate);
            Debug.Log(this.m_parentStatusEffect?.EffectPotency.ToString() ?? "Effect is Null");

            DelayedDamage.OnDelayedDamageTaken(_affectedCharacter, this.m_parentStatusEffect?.OwnerCharacter ?? null, damage);

            _affectedCharacter.Stats.AffectHealth(damage);

        }
    }

}