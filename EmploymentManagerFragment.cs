using EmploymentAutomation.UI;
using TimberApi.UIBuilderSystem;
using Timberborn.BaseComponentSystem;
using Timberborn.EntityPanelSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmploymentAutomation
{
    public class EmploymentManagerFragment : IEntityPanelFragment
    {
        private const string PercentageFormat = "P0";

        private readonly UIBuilder builder;
        private MinMaxSlider inStock;
        private Toggle inStockToggle;
        private EmploymentManagerComponent manager;
        private MinMaxSlider outStock;
        private Toggle outStockToggle;
        private MinMaxSlider power;
        private Toggle powerToggle;
        private VisualElement root;

        public EmploymentManagerFragment(UIBuilder builder)
        {
            this.builder = builder;
        }

        public void ClearFragment()
        {
            SetVisibility(root, manager != null && manager.available);
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
            var available = manager != null && manager.available;
            if (available)
            {
                powerToggle.value = manager.powerActive;
                power.value = new Vector2(manager.powerLow, manager.powerHigh);
                outStockToggle.value = manager.outStockActive;
                outStock.value = new Vector2(manager.outStockLow, manager.outStockHigh);
                outStock.label = manager.outStockLow.ToString(PercentageFormat) + " - " +
                                 manager.outStockHigh.ToString(PercentageFormat);
                inStockToggle.value = manager.inStockActive;
                inStock.value = new Vector2(manager.inStockLow, manager.inStockHigh);
                inStock.label = manager.inStockLow.ToString(PercentageFormat) + " - " +
                                manager.inStockHigh.ToString(PercentageFormat);
            }

            SetVisibility(root, available);
        }

        public void UpdateFragment()
        {
            var available = manager != null && manager.available;
            if (available)
            {
                inStock.label = manager.inStockLow.ToString(PercentageFormat) + " - " +
                                manager.inStockHigh.ToString(PercentageFormat);
                outStock.label = manager.outStockLow.ToString(PercentageFormat) + " - " +
                                 manager.outStockHigh.ToString(PercentageFormat);
                power.label = manager.powerLow.ToString(PercentageFormat) + " - " +
                              manager.powerHigh.ToString(PercentageFormat);
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

            SetVisibility(powerToggle, available && manager.powerAvailable);
            SetVisibility(outStockToggle, available && manager.outStockAvailable);
            SetVisibility(inStockToggle, available && manager.inStockAvailable);
            SetVisibility(power, available && powerToggle.value);
            SetVisibility(outStock, available && outStockToggle.value);
            SetVisibility(inStock, available && inStockToggle.value);
        }

        private static void SetVisibility(VisualElement element, bool visible)
        {
            element.visible = visible;
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}