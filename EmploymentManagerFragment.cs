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
        private Toggle powerToggle;
        private MinMaxSlider outStock;
        private MinMaxSlider inStock;
        private MinMaxSlider power;
        private EmploymentManagerComponent manager;

        public EmploymentManagerFragment(UIBuilder builder)
        {
            this.builder = builder;
        }

        public void ClearFragment()
        {
            SetVisibility(root, manager != null && manager.availible);
        }

        public VisualElement InitializeFragment()
        {
            root = builder.BuildAndInitialize<EmploymentManagerPanel>();
            powerToggle = root.Q<Toggle>("power_toggle");
            outStockToggle = root.Q<Toggle>("out_stock_toggle");
            inStockToggle = root.Q<Toggle>("in_stock_toggle");
            outStock = root.Q<MinMaxSlider>("out_stock");
            inStock = root.Q<MinMaxSlider>("in_stock");
            power = root.Q<MinMaxSlider>("power");
            SetVisibility(root, false);
            return root;
        }

        public void ShowFragment(BaseComponent entity)
        {
            manager = entity.GetComponentFast<EmploymentManagerComponent>();
            bool availible = manager != null && manager.availible;
            if (availible)
            {
                powerToggle.value = manager.powerActive;
                power.value = new Vector2(manager.powerLow, manager.powerHigh);
                outStockToggle.value = manager.outStockActive;
                outStock.value = new Vector2(manager.outStockLow, manager.outStockHigh);
                outStock.label = manager.outStockLow.ToString("0.00") + " - " + manager.outStockHigh.ToString("0.00");
                inStockToggle.value = manager.inStockActive;
                inStock.value = new Vector2(manager.inStockLow, manager.inStockHigh);
                inStock.label = manager.inStockLow.ToString("0.00") + " - " + manager.inStockHigh.ToString("0.00");
            }
            SetVisibility(root, availible);
        }

        public void UpdateFragment()
        {
            bool availible = manager != null && manager.availible;
            if (availible)
            {
                inStock.label = manager.inStockLow.ToString("0.00") + " - " + manager.inStockHigh.ToString("0.00");
                outStock.label = manager.outStockLow.ToString("0.00") + " - " + manager.outStockHigh.ToString("0.00");
                power.label = manager.powerLow.ToString("0.00") + " - " + manager.powerHigh.ToString("0.00");
                manager.powerActive = powerToggle.value;
                manager.outStockActive = outStockToggle.value;
                manager.inStockActive = inStockToggle.value;
                manager.powerLow = power.value.x;
                manager.powerHigh = power.value.y;
                manager.outStockLow = outStock.value.x;
                manager.outStockHigh = outStock.value.y;
                manager.inStockLow = inStock.value.x;
                manager.inStockHigh = inStock.value.y;
            }
            SetVisibility(powerToggle, availible && manager.powerAvailible);
            SetVisibility(outStockToggle, availible && manager.outStockAvailible);
            SetVisibility(inStockToggle, availible && manager.inStockAvailible);
            SetVisibility(power, availible && powerToggle.value);
            SetVisibility(outStock, availible && outStockToggle.value);
            SetVisibility(inStock, availible && inStockToggle.value);
        }

        private static void SetVisibility(VisualElement element, bool visible)
        {
            element.visible = visible;
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
