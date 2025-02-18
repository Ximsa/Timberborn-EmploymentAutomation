using Bindito.Core;
using EmploymentAutomation.UI;
using Timberborn.EntityPanelSystem;
using Timberborn.TemplateSystem;
using Timberborn.Workshops;

namespace EmploymentAutomation
{
    [Context("Game")]
    public class EmploymentAutomationConfigurator : Configurator
    {
        protected override void Configure()
        {
            Bind<DistrictResourceCounterService>().AsSingleton();
            Bind<EmploymentManagerPanel>().AsTransient();
            Bind<PanelFragment>().AsTransient();
            Bind<EmploymentManagerFragment>().AsTransient();
            MultiBind<EntityPanelModule>().ToProvider<EntityPanelModuleProvider>().AsSingleton();
            MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
        }

        private static TemplateModule ProvideTemplateModule()
        {
            var builder = new TemplateModule.Builder();
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
                var builder = new EntityPanelModule.Builder();
                builder.AddMiddleFragment(employmentManagerFragment);
                return builder.Build();
            }
        }
    }
}