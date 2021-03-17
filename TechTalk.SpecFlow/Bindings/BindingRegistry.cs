using System;
using System.Collections.Generic;
using System.Linq;

namespace TechTalk.SpecFlow.Bindings
{
    public interface IBindingRegistry
    {
        bool Ready { get; set; }

        IEnumerable<IStepDefinitionBinding> GetStepDefinitions();
        IEnumerable<IHookBinding> GetHooks();
        IEnumerable<IStepDefinitionBinding> GetConsideredStepDefinitions(StepDefinitionType stepDefinitionType, string stepText = null);
        IEnumerable<IHookBinding> GetHooks(HookType bindingEvent);
        IEnumerable<IStepArgumentTransformationBinding> GetStepTransformations();

        void RegisterStepDefinitionBinding(IStepDefinitionBinding stepDefinitionBinding);
        void RegisterHookBinding(IHookBinding hookBinding);
        void RegisterStepArgumentTransformationBinding(IStepArgumentTransformationBinding stepArgumentTransformationBinding);
    }

    public class BindingRegistry : IBindingRegistry
    {
        private readonly List<IStepDefinitionBinding> stepDefinitions = new List<IStepDefinitionBinding>();
        private readonly List<IStepArgumentTransformationBinding> stepArgumentTransformations = new List<IStepArgumentTransformationBinding>();
        private readonly Dictionary<HookType, List<IHookBinding>> hooks = new Dictionary<HookType, List<IHookBinding>>();

        public bool Ready { get; set; }

        public IEnumerable<IStepDefinitionBinding> GetStepDefinitions()
        {
            return stepDefinitions;
        }

        public IEnumerable<IStepDefinitionBinding> GetConsideredStepDefinitions(StepDefinitionType stepDefinitionType, string stepText)
        {
            //TODO: later optimize to return step definitions that has a chance to match to stepText
            return stepDefinitions.Where(sd => sd.StepDefinitionType == stepDefinitionType);
        }

        public virtual IEnumerable<IHookBinding> GetHooks()
        {
            return hooks.Values.SelectMany(hookList => hookList);
        }

        public virtual IEnumerable<IHookBinding> GetHooks(HookType bindingEvent)
        {
            if (hooks.TryGetValue(bindingEvent, out var list))
            {
                return list;
            }

            return Array.Empty<IHookBinding>();
        }

        public virtual IEnumerable<IStepArgumentTransformationBinding> GetStepTransformations()
        {
            return stepArgumentTransformations;
        }

        public virtual void RegisterStepDefinitionBinding(IStepDefinitionBinding stepDefinitionBinding)
        {
            stepDefinitions.Add(stepDefinitionBinding);
        }

        public virtual void RegisterHookBinding(IHookBinding hookBinding)
        {
            if (hooks.TryGetValue(hookBinding.HookType, out var list))
            {
                if (!list.Contains(hookBinding))
                {
                    list.Add(hookBinding);
                }
            }
            else
            {
                list = new List<IHookBinding>(1) { hookBinding };
                hooks.Add(hookBinding.HookType, list);
            }
        }

        public virtual void RegisterStepArgumentTransformationBinding(IStepArgumentTransformationBinding stepArgumentTransformationBinding)
        {
            stepArgumentTransformations.Add(stepArgumentTransformationBinding);
        }
    }
}