using System;
using System.Collections.Generic;
using EmploymentAutomation.Logic;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using Timberborn.Workshops;
using TimberUi.CommonUi;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmploymentAutomation.UI;

public class IngredientAutomationFragment(
    ILoc loc,
    VisualElementInitializer initializer) : BaseEntityPanelFragment<IngredientComponent>
{
    private Toggle toggle;
    private MinMaxSlider slider;

    protected override void InitializePanel()
    {
        toggle = panel.AddToggle(
            text: ToggleText(0),
            onValueChanged: OnToggle);
        slider = panel.AddIntMinMaxSliderWithValueDisplay(
            label: "",
            value: new Vector2Int(),
            min: 0,
            max: 100,
            onChange: OnIngredientSliderChanged);
        panel.Initialize(initializer);
    }

    public override void ShowFragment(BaseComponent entity)
    {
        Console.WriteLine("-----");
        Console.WriteLine("ShowFragment");
        base.ShowFragment(entity);
        if (component == null) return;
        Console.WriteLine("ShowFragment");
        Update(component);
    }

    public override void UpdateFragment()
    {
        Console.WriteLine("-----");
        Console.WriteLine("UpdateFragment");
        if (component == null) return;
        Console.WriteLine("UpdateFragment");
        Update(component);
    }

    private void Update(IngredientComponent component)
    {
        toggle.ToggleDisplayStyle(component.Available);
        slider.ToggleDisplayStyle(component.Available);
        toggle.text = ToggleText(component.Fillrate);
        toggle.value = component.InStockActive;
        slider.value = new Vector2(component.InStockLow, component.InStockHigh);
    }

    private static string ToggleText(float fillrate) =>
        "Enable Ingredients. Fillrate:" + (int)Math.Round(fillrate) + "%";

    private void OnIngredientSliderChanged(Vector2Int value)
    {
        if (component == null)
            return;
        component.InStockLow = value.x / 100f;
        component.InStockHigh = value.y / 100f;
    }

    private void OnToggle(bool toggleState)
    {
        if (component == null)
            return;
        component.InStockActive = toggleState;
    }
}