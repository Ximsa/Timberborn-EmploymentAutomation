
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
			visualElementBuilder.AddComponent(UIBuilder.Create<GameTextToggle>()
				.SetName("power_toggle")
				.SetText("Pause without power")
				.Build());
			visualElementBuilder.AddComponent(UIBuilder.Create<GameTextToggle>()
				.SetName("out_stock_toggle")
				.SetText("Automate products")
				.Build());
			visualElementBuilder.AddComponent(UIBuilder.Create<GameMinMaxSlider>()
				.Small()
				.SetName("out_stock")
				.SetLocKey("Ximsa.EmploymentAutomation.None")
				.SetLowLimit(0)
				.SetHighLimit(1)
				.Build());
			visualElementBuilder.AddComponent(UIBuilder.Create<GameTextToggle>()
				.SetName("in_stock_toggle")
				.SetText("Automate ingredients")
				.Build());
			visualElementBuilder.AddComponent(UIBuilder.Create<GameMinMaxSlider>()
				.Small()
				.SetName("in_stock")
				.SetLocKey("Ximsa.EmploymentAutomation.None")
				.SetLowLimit(0)
				.SetHighLimit(1)
				.Build());
			visualElementBuilder.SetFlexDirection(FlexDirection.Column);
			visualElementBuilder.SetWidth(new Length(100f, LengthUnit.Percent));
			visualElementBuilder.SetJustifyContent(Justify.Center);
			return visualElementBuilder.BuildAndInitialize();
		}
	}
}
