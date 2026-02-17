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

public class PowerAutomationFragment(
    ILoc loc,
    VisualElementInitializer initializer) : BaseEntityPanelFragment<PowerComponent>
{
    private Toggle toggle;
    private MinMaxSlider slider;

    protected override void InitializePanel()
    {
        toggle = panel.AddToggle(text: "Enable Power", onValueChanged: OnToggle);
        slider = panel.AddIntMinMaxSliderWithValueDisplay(
            label: "",
            value: new Vector2Int(10, 50),
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

    private void Update(PowerComponent component)
    {
        toggle.ToggleDisplayStyle(component.Available);
        slider.ToggleDisplayStyle(component.Available);
        toggle.text = ToggleText(0);
        toggle.value = component.PowerActive;
        slider.value = new Vector2(component.PowerLow, component.PowerHigh);
    }

    private static string ToggleText(float fillrate) =>
        "Enable Ingredients. Fillrate:" + (int)Math.Round(fillrate) + "%";

    private void OnIngredientSliderChanged(Vector2Int value)
    {
        if (component == null)
            return;
        component.PowerLow = value.x / 100f;
        component.PowerHigh = value.y / 100f;
    }

    private void OnToggle(bool toggleState)
    {
        if (component == null)
            return;
        component.PowerActive = toggleState;
    }
}