using Bindito.Core;
using EmploymentAutomation.UIPresets;
using System;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateSystem;
using Timberborn.Workshops;

namespace EmploymentAutomation
{
    [Context("Game")]
    public class Configurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            containerDefinition.Bind<DistrictResourceCounterService>().AsSingleton();
            containerDefinition.Bind<EmploymentManagerPanel>().AsTransient();
            containerDefinition.Bind<PanelFragment>().AsTransient();
            containerDefinition.Bind<EmploymentManagerFragment>().AsTransient();
            containerDefinition.MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
            containerDefinition.MultiBind<TemplateModule>().ToProvider(new Func<TemplateModule>(ProvideTemplateModule)).AsSingleton();
        }

        private static TemplateModule ProvideTemplateModule()
        {
            TemplateModule.Builder builder = new TemplateModule.Builder();
            builder.AddDecorator<Manufactory, EmploymentManagerComponent>();
            return builder.Build();
        }

        private class EntityPanelModuleProvider : IProvider<EntityPanelModule>
        {
            private readonly EmploymentManagerFragment employmentManagerFragment;

            public EntityPanelModuleProvider(EmploymentManagerFragment employmentManagerFragment)
            {
                this.employmentManagerFragment = employmentManagerFragment;
            }

            public EntityPanelModule Get()
            {
                EntityPanelModule.Builder builder = new EntityPanelModule.Builder();
                builder.AddMiddleFragment(employmentManagerFragment);
                return builder.Build();
            }
        }
    }
}
