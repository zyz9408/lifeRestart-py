using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

namespace LifeRestart.Udon
{
    public enum PropertyKey
    {
        CHR,
        INT,
        STR,
        MNY,
        SPR,
        AGE,
        LIF,
        TOTAL,
    }

    [Serializable]
    public class PropertyEffect
    {
        public int chr;
        public int intel;
        public int str;
        public int money;
        public int spirit;
        public int life;
        public int age;
        public int total;
        public int randomBonus;

        public bool HasAnyEffect()
        {
            return chr != 0 || intel != 0 || str != 0 || money != 0 || spirit != 0 ||
                   life != 0 || age != 0 || total != 0 || randomBonus != 0;
        }
    }

    [Serializable]
    public class PropertyState
    {
        public int CHR;
        public int INT;
        public int STR;
        public int MNY;
        public int SPR = 5;
        public int AGE = -1;
        public int LIF = 1;
        public int Total = 20;
        public int TMS = 1;

        public void Reset()
        {
            CHR = 0;
            INT = 0;
            STR = 0;
            MNY = 0;
            SPR = 5;
            AGE = -1;
            LIF = 1;
            Total = 20;
            TMS = 1;
        }

        public int Get(PropertyKey key)
        {
            switch (key)
            {
                case PropertyKey.CHR: return CHR;
                case PropertyKey.INT: return INT;
                case PropertyKey.STR: return STR;
                case PropertyKey.MNY: return MNY;
                case PropertyKey.SPR: return SPR;
                case PropertyKey.AGE: return AGE;
                case PropertyKey.LIF: return LIF;
                case PropertyKey.TOTAL: return Total;
                default: return 0;
            }
        }

        public void Apply(PropertyEffect effect, System.Random random)
        {
            if (effect == null || !effect.HasAnyEffect())
            {
                return;
            }

            CHR += effect.chr;
            INT += effect.intel;
            STR += effect.str;
            MNY += effect.money;
            SPR += effect.spirit;
            LIF += effect.life;
            AGE += effect.age;
            Total += effect.total;

            if (effect.randomBonus != 0)
            {
                var roll = random.Next(0, 5);
                switch (roll)
                {
                    case 0:
                        CHR += effect.randomBonus;
                        break;
                    case 1:
                        INT += effect.randomBonus;
                        break;
                    case 2:
                        STR += effect.randomBonus;
                        break;
                    case 3:
                        MNY += effect.randomBonus;
                        break;
                    default:
                        SPR += effect.randomBonus;
                        break;
                }
            }
        }
    }

    public enum ConditionMode
    {
        None,
        PropertyGreaterOrEqual,
        PropertyGreater,
        PropertyLessOrEqual,
        PropertyLess,
        PropertyEqual,
        PropertyNotEqual,
        HasTalent,
        NotHasTalent,
        HasEvent,
        NotHasEvent,
    }

    [Serializable]
    public class ConditionDefinition
    {
        public ConditionMode mode;
        public PropertyKey property;
        public int threshold;
        public int[] idList;

        public bool Evaluate(PropertyState propertyState, TalentRuntimeState talentState, EventRuntimeState eventState)
        {
            switch (mode)
            {
                case ConditionMode.None:
                    return true;
                case ConditionMode.PropertyGreaterOrEqual:
                    return propertyState.Get(property) >= threshold;
                case ConditionMode.PropertyGreater:
                    return propertyState.Get(property) > threshold;
                case ConditionMode.PropertyLessOrEqual:
                    return propertyState.Get(property) <= threshold;
                case ConditionMode.PropertyLess:
                    return propertyState.Get(property) < threshold;
                case ConditionMode.PropertyEqual:
                    return propertyState.Get(property) == threshold;
                case ConditionMode.PropertyNotEqual:
                    return propertyState.Get(property) != threshold;
                case ConditionMode.HasTalent:
                    return talentState.ContainsAny(idList);
                case ConditionMode.NotHasTalent:
                    return !talentState.ContainsAny(idList);
                case ConditionMode.HasEvent:
                    return eventState.ContainsAny(idList);
                case ConditionMode.NotHasEvent:
                    return !eventState.ContainsAny(idList);
                default:
                    return false;
            }
        }
    }

    [Serializable]
    public class ConditionGroup
    {
        public ConditionDefinition[] all;
        public ConditionDefinition[] any;

        public bool Evaluate(PropertyState propertyState, TalentRuntimeState talentState, EventRuntimeState eventState)
        {
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (!all[i].Evaluate(propertyState, talentState, eventState))
                    {
                        return false;
                    }
                }
            }

            if (any != null && any.Length > 0)
            {
                for (int i = 0; i < any.Length; i++)
                {
                    if (any[i].Evaluate(propertyState, talentState, eventState))
                    {
                        return true;
                    }
                }

                return false;
            }

            return true;
        }
    }

    [Serializable]
    public class TalentDefinition
    {
        public int id;
        public string displayName;
        [TextArea]
        public string description;
        [Range(0, 3)]
        public int grade;
        public int statusBonus;
        public int[] exclusive;
        public PropertyEffect effect;
        public ConditionGroup condition;
    }

    [Serializable]
    public class EventBranchDefinition
    {
        public ConditionGroup condition;
        public int nextEventId;
    }

    [Serializable]
    public class EventDefinition
    {
        public int id;
        [TextArea]
        public string description;
        public bool noRandom;
        public ConditionGroup includeCondition;
        public ConditionGroup excludeCondition;
        public PropertyEffect effect;
        public string postEventText;
        public EventBranchDefinition[] branches;
    }

    [Serializable]
    public class WeightedEventDefinition
    {
        public int eventId;
        public float weight;
    }

    [Serializable]
    public class AgeDefinition
    {
        public int age;
        public WeightedEventDefinition[] events;
        public int[] grantedTalentIds;
    }

    [Serializable]
    public class TalentRuntimeState
    {
        public const int MaxTalents = 32;

        [SerializeField]
        private int[] activeIds = new int[MaxTalents];

        [SerializeField]
        private int activeCount = 0;

        [SerializeField]
        private int[] triggeredIds = new int[MaxTalents];

        [SerializeField]
        private int triggeredCount = 0;

        public void Reset()
        {
            activeCount = 0;
            triggeredCount = 0;
        }

        public bool ContainsAny(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < ids.Length; i++)
            {
                if (Contains(ids[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(int id)
        {
            for (int i = 0; i < activeCount; i++)
            {
                if (activeIds[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsTriggered(int id)
        {
            for (int i = 0; i < triggeredCount; i++)
            {
                if (triggeredIds[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        public void MarkTriggered(int id)
        {
            if (IsTriggered(id))
            {
                return;
            }

            if (triggeredCount < triggeredIds.Length)
            {
                triggeredIds[triggeredCount++] = id;
            }
        }

        public void AddTalent(int id)
        {
            if (Contains(id) || activeCount >= activeIds.Length)
            {
                return;
            }

            activeIds[activeCount++] = id;
        }

        public int[] GetActiveIds()
        {
            var ids = new int[activeCount];
            Array.Copy(activeIds, ids, activeCount);
            return ids;
        }
    }

    [Serializable]
    public class EventRuntimeState
    {
        public const int MaxEvents = 256;

        [SerializeField]
        private int[] triggeredIds = new int[MaxEvents];

        [SerializeField]
        private int triggeredCount = 0;

        public void Reset()
        {
            triggeredCount = 0;
        }

        public bool ContainsAny(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < ids.Length; i++)
            {
                if (Contains(ids[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Contains(int id)
        {
            for (int i = 0; i < triggeredCount; i++)
            {
                if (triggeredIds[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        public void MarkTriggered(int id)
        {
            if (Contains(id) || triggeredCount >= triggeredIds.Length)
            {
                return;
            }

            triggeredIds[triggeredCount++] = id;
        }
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LifeGameController : UdonSharpBehaviour
    {
        private const int TalentChoicesPerRun = 3;
        private const int TalentCandidates = 10;

        [Header("Configuration")]
        public TalentDefinition[] talentDefinitions;
        public AgeDefinition[] ageDefinitions;
        public EventDefinition[] eventDefinitions;
        public int inheritTalentId = -1;
        public bool autoStart;
        public int randomSeed = -1;

        [Header("Runtime State")]
        public PropertyState propertyState = new PropertyState();
        public TalentRuntimeState talentState = new TalentRuntimeState();
        public EventRuntimeState eventState = new EventRuntimeState();

        [Header("Debug Output")]
        [TextArea]
        public string lastLog;

        private System.Random _random;
        private TalentDefinition[][] _talentByGrade;

        private void Start()
        {
            BuildLookups();

            if (autoStart)
            {
                BeginLife();
            }
        }

        public void BuildLookups()
        {
            _talentByGrade = new TalentDefinition[4][];

            for (int grade = 0; grade < _talentByGrade.Length; grade++)
            {
                int count = 0;
                for (int i = 0; i < talentDefinitions.Length; i++)
                {
                    if (talentDefinitions[i].grade == grade)
                    {
                        count++;
                    }
                }

                var buffer = new TalentDefinition[count];
                int index = 0;
                for (int i = 0; i < talentDefinitions.Length; i++)
                {
                    if (talentDefinitions[i].grade == grade)
                    {
                        buffer[index++] = talentDefinitions[i];
                    }
                }

                _talentByGrade[grade] = buffer;
            }
        }

        private void EnsureRandom()
        {
            if (_random == null)
            {
                if (randomSeed >= 0)
                {
                    _random = new System.Random(randomSeed);
                }
                else
                {
                    _random = new System.Random((int)DateTime.UtcNow.Ticks);
                }
            }
        }

        public void BeginLife()
        {
            EnsureRandom();
            propertyState.Reset();
            talentState.Reset();
            eventState.Reset();
            propertyState.TMS = inheritTalentId > 0 ? propertyState.TMS + 1 : 1;

            if (inheritTalentId >= 0)
            {
                talentState.AddTalent(inheritTalentId);
            }

            AllocateInitialProperties(propertyState.Total);
            ChooseTalents();
            RunLifeToEnd();
        }

        public void AllocateInitialProperties(int total)
        {
            int remaining = Mathf.Max(total, 0);
            int[] values = new int[4];

            for (int i = 0; i < 3; i++)
            {
                int max = Mathf.Max(10, 1);
                int value = Mathf.Min(10, remaining > 0 ? _random.Next(0, remaining + 1) : 0);
                values[i] = value;
                remaining -= value;
                if (remaining < 0)
                {
                    remaining = 0;
                }
            }

            values[3] = remaining;

            propertyState.CHR = values[0];
            propertyState.INT = values[1];
            propertyState.STR = values[2];
            propertyState.MNY = values[3];
        }

        public void ChooseTalents()
        {
            var candidates = new TalentDefinition[TalentCandidates];
            int candidateCount = 0;

            while (candidateCount < TalentCandidates)
            {
                var talent = PickRandomTalent();
                bool exists = false;
                for (int i = 0; i < candidateCount; i++)
                {
                    if (candidates[i].id == talent.id)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    candidates[candidateCount++] = talent;
                }
            }

            if (inheritTalentId >= 0)
            {
                var inherited = FindTalent(inheritTalentId);
                if (inherited != null)
                {
                    AddTalent(inherited);
                }
            }

            for (int i = 0; i < TalentChoicesPerRun && i < candidateCount; i++)
            {
                AddTalent(candidates[i]);
            }
        }

        private TalentDefinition PickRandomTalent()
        {
            float value = (float)_random.NextDouble();
            int grade = 3;
            float[] prob = { 0.889f, 0.1f, 0.01f, 0.001f };

            while (grade >= 0)
            {
                value -= prob[grade];
                if (value <= 0)
                {
                    break;
                }

                grade--;
            }

            if (grade < 0)
            {
                grade = 0;
            }

            var pool = _talentByGrade[Mathf.Clamp(grade, 0, _talentByGrade.Length - 1)];
            if (pool.Length == 0)
            {
                return talentDefinitions.Length > 0 ? talentDefinitions[0] : null;
            }

            int index = _random.Next(0, pool.Length);
            return pool[index];
        }

        private void AddTalent(TalentDefinition talent)
        {
            if (talent == null)
            {
                return;
            }

            if (talent.exclusive != null)
            {
                for (int i = 0; i < talent.exclusive.Length; i++)
                {
                    if (talentState.Contains(talent.exclusive[i]))
                    {
                        return;
                    }
                }
            }

            talentState.AddTalent(talent.id);
            propertyState.Total += talent.statusBonus;
        }

        public void RunLifeToEnd()
        {
            lastLog = string.Empty;
            while (propertyState.LIF > 0)
            {
                AdvanceYear();
            }
        }

        public void AdvanceYear()
        {
            propertyState.AGE += 1;
            var age = FindAgeDefinition(propertyState.AGE);

            if (age != null && age.grantedTalentIds != null)
            {
                for (int i = 0; i < age.grantedTalentIds.Length; i++)
                {
                    var talent = FindTalent(age.grantedTalentIds[i]);
                    if (talent != null)
                    {
                        AddTalent(talent);
                    }
                }
            }

            if (age != null && age.events != null && age.events.Length > 0)
            {
                var evt = ChooseEvent(age.events);
                if (evt != null)
                {
                    RunEvent(evt);
                }
            }
        }

        private AgeDefinition FindAgeDefinition(int age)
        {
            for (int i = 0; i < ageDefinitions.Length; i++)
            {
                if (ageDefinitions[i].age == age)
                {
                    return ageDefinitions[i];
                }
            }

            return null;
        }

        private TalentDefinition FindTalent(int id)
        {
            for (int i = 0; i < talentDefinitions.Length; i++)
            {
                if (talentDefinitions[i].id == id)
                {
                    return talentDefinitions[i];
                }
            }

            return null;
        }

        private EventDefinition FindEvent(int id)
        {
            for (int i = 0; i < eventDefinitions.Length; i++)
            {
                if (eventDefinitions[i].id == id)
                {
                    return eventDefinitions[i];
                }
            }

            return null;
        }

        private EventDefinition ChooseEvent(WeightedEventDefinition[] events)
        {
            float totalWeight = 0f;
            for (int i = 0; i < events.Length; i++)
            {
                var evt = FindEvent(events[i].eventId);
                if (evt == null)
                {
                    continue;
                }

                if (!evt.noRandom && evt.includeCondition != null && !evt.includeCondition.Evaluate(propertyState, talentState, eventState))
                {
                    continue;
                }

                if (evt.excludeCondition != null && evt.excludeCondition.Evaluate(propertyState, talentState, eventState))
                {
                    continue;
                }

                totalWeight += Mathf.Max(0f, events[i].weight);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = (float)_random.NextDouble() * totalWeight;

            for (int i = 0; i < events.Length; i++)
            {
                var evt = FindEvent(events[i].eventId);
                if (evt == null)
                {
                    continue;
                }

                if (!evt.noRandom && evt.includeCondition != null && !evt.includeCondition.Evaluate(propertyState, talentState, eventState))
                {
                    continue;
                }

                if (evt.excludeCondition != null && evt.excludeCondition.Evaluate(propertyState, talentState, eventState))
                {
                    continue;
                }

                roll -= Mathf.Max(0f, events[i].weight);
                if (roll <= 0f)
                {
                    return evt;
                }
            }

            return FindEvent(events[0].eventId);
        }

        private void RunEvent(EventDefinition evt)
        {
            if (evt == null)
            {
                return;
            }

            eventState.MarkTriggered(evt.id);
            propertyState.Apply(evt.effect, _random);
            AppendLog(evt.description);

            if (evt.branches != null)
            {
                for (int i = 0; i < evt.branches.Length; i++)
                {
                    var branch = evt.branches[i];
                    if (branch.condition == null || branch.condition.Evaluate(propertyState, talentState, eventState))
                    {
                        RunEvent(FindEvent(branch.nextEventId));
                        return;
                    }
                }
            }

            if (!string.IsNullOrEmpty(evt.postEventText))
            {
                AppendLog(evt.postEventText);
            }
        }

        private void AppendLog(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (string.IsNullOrEmpty(lastLog))
            {
                lastLog = text;
            }
            else
            {
                lastLog += "\n" + text;
            }
        }
    }
}
