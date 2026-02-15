using Bindito.Core;
using EmploymentAutomation.Logic;
using Timberborn.TemplateInstantiation;
using Timberborn.Workshops;
using TimberUi.CommonProviders;

namespace EmploymentAutomation.Configuration;

[Context("Game")]
public class GameConfigurator : Configurator
{
    public class FragmentsProvider(AutomationFragment fragment) : EntityPanelFragmentProvider([fragment]);

    protected override void Configure()
    {
        Bind<DistrictResourceCounterService>().AsSingleton();
        Bind<EmploymentComponent>().AsSingleton();
        Bind<PowerComponent>().AsSingleton();
        Bind<IngredientComponent>().AsSingleton();
        Bind<ProductComponent>().AsSingleton();
        MultiBind<TemplateModule>().ToProvider(ProvideTemplateModule).AsSingleton();
        this.BindFragments<FragmentsProvider>();
    }

    private static TemplateModule ProvideTemplateModule()
    {
        var builder = new TemplateModule.Builder();
        builder.AddDecorator<Manufactory, EmploymentComponent>();
        builder.AddDecorator<Manufactory, PowerComponent>();
        builder.AddDecorator<Manufactory, IngredientComponent>();
        builder.AddDecorator<Manufactory, ProductComponent>();
        return builder.Build();
    }
}