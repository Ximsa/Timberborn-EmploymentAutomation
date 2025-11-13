using Bindito.Core;
using Timberborn.TemplateInstantiation;
using Timberborn.Workshops;
using TimberUi.CommonProviders;

namespace EmploymentAutomation
{
    [Context("Game")]
    public class GameConfigurator : Configurator
    {
        public class FragmentsProvider(AutomationFragment fragment) : EntityPanelFragmentProvider([fragment]);

        protected override void Configure()
        {
            Bind<DistrictResourceCounterService>().AsSingleton();
            this.BindFragments<FragmentsProvider>();
        }
    }
}