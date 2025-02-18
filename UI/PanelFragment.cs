using System;
using TimberApi.UIBuilderSystem;
using TimberApi.UIBuilderSystem.ElementBuilders;
using TimberApi.UIBuilderSystem.StyleSheetSystem;
using TimberApi.UIBuilderSystem.StylingElements;
using Timberborn.CoreUI;
using UnityEngine.UIElements;

namespace EmploymentAutomation.UI
{
    public class PanelFragment : PanelFragment<PanelFragment>
    {
        protected override PanelFragment BuilderInstance => this;
    }

    public abstract class PanelFragment<TBuilder> : BaseBuilder<TBuilder, NineSliceVisualElement>
        where TBuilder : BaseBuilder<TBuilder, NineSliceVisualElement>
    {
        private const string BackgroundClass = "PanelFragment";

        private VisualElementBuilder visualElementBuilder;

        protected override NineSliceVisualElement InitializeRoot()
        {
            visualElementBuilder = UIBuilder.Create<VisualElementBuilder>();
            visualElementBuilder.AddClass(BackgroundClass);
            visualElementBuilder.SetPadding(
                new Padding(new Length(12f, LengthUnit.Pixel),
                    new Length(8f, LengthUnit.Pixel)));
            return visualElementBuilder.Build();
        }

        public TBuilder AddComponent(Type builderType)
        {
            Root.Add(UIBuilder.Build(builderType));
            return BuilderInstance;
        }

        public TBuilder AddComponent(VisualElement visualElement)
        {
            Root.Add(visualElement);
            return BuilderInstance;
        }

        public TBuilder SetFlexDirection(FlexDirection direction)
        {
            Root.style.flexDirection = direction;
            return BuilderInstance;
        }

        public TBuilder SetWidth(Length width)
        {
            Root.style.width = width;
            return BuilderInstance;
        }

        public TBuilder SetJustifyContent(Justify justify)
        {
            Root.style.justifyContent = justify;
            return BuilderInstance;
        }

        protected override void InitializeStyleSheet(StyleSheetBuilder styleSheetBuilder)
        {
            styleSheetBuilder.AddNineSlicedBackgroundClass(
                BackgroundClass, "ui/images/backgrounds/bg-3", 9f, 0.5f);
        }
    }
}