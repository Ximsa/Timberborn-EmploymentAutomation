using System;
using TimberApi.UIBuilderSystem;
using TimberApi.UIBuilderSystem.ElementBuilders;
using TimberApi.UIBuilderSystem.StyleSheetSystem;
using TimberApi.UIBuilderSystem.StylingElements;
using Timberborn.CoreUI;
using UnityEngine.UIElements;

namespace EmploymentAutomation.UIPresets
{
	public class PanelFragment : PanelFragment<PanelFragment>
	{
		protected override PanelFragment BuilderInstance
		{
			get
			{
				return this;
			}
		}
	}
	public abstract class PanelFragment<TBuilder> : BaseBuilder<TBuilder, NineSliceVisualElement> where TBuilder : BaseBuilder<TBuilder, NineSliceVisualElement>
	{
		private const string BackgroundClass = "PanelFragment";

		private VisualElementBuilder visualElementBuilder;
		protected override NineSliceVisualElement InitializeRoot()
		{
			this.visualElementBuilder = this.UIBuilder.Create<VisualElementBuilder>();
			this.visualElementBuilder.AddClass(BackgroundClass);
			this.visualElementBuilder.SetPadding(new Padding(new Length(12f, LengthUnit.Pixel), new Length(8f, LengthUnit.Pixel)));
			return this.visualElementBuilder.Build();
		}

		public TBuilder AddComponent(Type builderType)
		{
			base.Root.Add(this.UIBuilder.Build(builderType));
			return this.BuilderInstance;
		}

		public TBuilder AddComponent(VisualElement visualElement)
		{
			base.Root.Add(visualElement);
			return this.BuilderInstance;
		}

		public TBuilder SetFlexDirection(FlexDirection direction)
		{
			base.Root.style.flexDirection = direction;
			return this.BuilderInstance;
		}

		public TBuilder SetWidth(Length width)
		{
			base.Root.style.width = width;
			return this.BuilderInstance;
		}

		public TBuilder SetJustifyContent(Justify justify)
		{
			base.Root.style.justifyContent = justify;
			return this.BuilderInstance;
		}

		protected override void InitializeStyleSheet(StyleSheetBuilder styleSheetBuilder)
		{
			styleSheetBuilder.AddNineSlicedBackgroundClass(BackgroundClass, "ui/images/backgrounds/bg-3", 9f, 0.5f);
		}
	}
}
