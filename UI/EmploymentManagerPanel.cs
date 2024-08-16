
using TimberApi.UIBuilderSystem;
using TimberApi.UIPresets.Sliders;
using TimberApi.UIPresets.Toggles;
using Timberborn.CoreUI;
using UnityEngine.UIElements;

namespace EmploymentAutomation.UIPresets
{
	public class EmploymentManagerPanel : EmploymentManagerPanel<EmploymentManagerPanel>
	{
		protected override EmploymentManagerPanel BuilderInstance
		{
			get
			{
				return this;
			}
		}
	}
	public abstract class EmploymentManagerPanel<TBuilder> : BaseBuilder<TBuilder, NineSliceVisualElement> where TBuilder : BaseBuilder<TBuilder, NineSliceVisualElement>
	{
		private PanelFragment visualElementBuilder;
		protected override NineSliceVisualElement InitializeRoot()
		{
			visualElementBuilder = UIBuilder.Create<PanelFragment>();
			visualElementBuilder.AddComponent(UIBuilder.Create<GameToggle>()
				.SetName("power_toggle")
				.SetLocKey("Ximsa.EmploymentAutomation.PowerToggle")
				.Build());
			visualElementBuilder.AddComponent(UIBuilder.Create<GameToggle>()
				.SetName("in_stock_toggle")
				.SetLocKey("Ximsa.EmploymentAutomation.IngredientToggle")
				.Build());
			visualElementBuilder.AddComponent(UIBuilder.Create<GameTextMinMaxSlider>()
				.Small()
				.SetName("in_stock")
				.SetLabel("Ingredient")
				.SetLowLimit(0)
				.SetHighLimit(1)
				.Build());
			visualElementBuilder.AddComponent(UIBuilder.Create<GameToggle>()
				.SetName("out_stock_toggle")
				.SetLocKey("Ximsa.EmploymentAutomation.ProductToggle")
				.Build());
			visualElementBuilder.AddComponent(UIBuilder.Create<GameTextMinMaxSlider>()
				.Small()
				.SetName("out_stock")
				.SetLabel("Product")
				.SetLowLimit(0)
				.SetHighLimit(1)
				.Build());
			visualElementBuilder.SetFlexDirection(FlexDirection.Column);
			visualElementBuilder.SetWidth(new Length(100f, LengthUnit.Percent));
			visualElementBuilder.SetJustifyContent(Justify.SpaceBetween);
			return visualElementBuilder.BuildAndInitialize();
		}
	}
}
