using TimberApi.UIBuilderSystem;
using Timberborn.BaseComponentSystem;
using Timberborn.EntityPanelSystem;
using UnityEngine;
using UnityEngine.UIElements;
using EmploymentAutomation.UIPresets;

namespace EmploymentAutomation
{
    public class EmploymentManagerFragment : IEntityPanelFragment
    {
        private readonly UIBuilder builder;
        private VisualElement root;
        private Toggle outStockToggle;
        private Toggle inStockToggle;
        private MinMaxSlider outStock;
        private MinMaxSlider inStock;
        private EmploymentManagerComponent manager;

        public EmploymentManagerFragment(UIBuilder builder)
        {
            this.builder = builder;
        }

        public void ClearFragment()
        {
            root.visible = false;
        }

        public VisualElement InitializeFragment()
        {
            root = builder.BuildAndInitialize<EmploymentManagerPanel>();
            outStockToggle = root.Q<Toggle>("out_stock_toggle");
            inStockToggle = root.Q<Toggle>("in_stock_toggle");
            outStock = root.Q<MinMaxSlider>("out_stock");
            inStock = root.Q<MinMaxSlider>("in_stock");
            SetVisibility(root, false);
            return root;
        }

        public void ShowFragment(BaseComponent entity)
        {
            manager = entity.GetComponentFast<EmploymentManagerComponent>();
            bool availible = manager != null && manager.availible;
            if (availible)
            {
                outStockToggle.value = manager.outStockActive;
                outStock.value = new Vector2(manager.outStockLow, manager.outStockHigh);
                inStockToggle.value = manager.inStockActive;
                inStock.value = new Vector2(manager.inStockLow, manager.inStockHigh);
            }
            SetVisibility(root, availible);
        }

        public void UpdateFragment()
        {
            bool availible = manager != null && manager.availible;
            SetVisibility(outStockToggle, availible && manager.outStockAvailible);
            SetVisibility(inStockToggle, availible && manager.inStockAvailible);
            SetVisibility(outStock, availible && outStockToggle.value);
            SetVisibility(inStock, availible && inStockToggle.value);
            if (availible)
            {
                manager.outStockActive = outStockToggle.value;
                manager.inStockActive = inStockToggle.value;
                manager.outStockLow = outStock.value.x;
                manager.outStockHigh = outStock.value.y;
                manager.inStockLow = inStock.value.x;
                manager.inStockHigh = inStock.value.y;
                inStock.label = manager.inStockText;
                outStock.label = manager.outStockText;
            }
        }

        private static void SetVisibility(VisualElement element, bool visible)
        {
            element.visible = visible;
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
