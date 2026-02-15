using System;
using Bindito.Core;
using JetBrains.Annotations;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.WorkSystem;
using UnityEngine;

namespace EmploymentAutomation.Logic;

public class EmploymentComponent : TickableComponent
{
    [CanBeNull] private Workplace workplace;
    [CanBeNull] private PowerComponent powerComponent;
    [CanBeNull] private ProductComponent productComponent;
    [CanBeNull] private IngredientComponent ingredientComponent;
    private bool componentsAreDirty = true;

    [Inject]
    public void InjectDependencies(
        EventBus eventBus)
    {
        eventBus.Register(this);
    }

    private void UpdateComponents()
    {
        workplace = HasComponent<Workplace>() ? GetComponent<Workplace>() : null;
        powerComponent = HasComponent<PowerComponent>() ? GetComponent<PowerComponent>() : null;
        productComponent = HasComponent<ProductComponent>() ? GetComponent<ProductComponent>() : null;
        ingredientComponent = HasComponent<IngredientComponent>() ? GetComponent<IngredientComponent>() : null;
    }

    public override void Tick()
    {
        if (componentsAreDirty)
        {
            UpdateComponents();
            componentsAreDirty = false;
        }

        var ingredientsEnabled = ingredientComponent is { Available: true };
        var productEnabled = productComponent is { Available: true };
        var powerEnabled = powerComponent is { Available: true };

        var hasToTick = workplace is not null && (ingredientsEnabled || productEnabled || powerEnabled);
        Console.WriteLine(hasToTick);
        if (!hasToTick) return;
        Console.WriteLine(05);
        // employment trigger bounds
        var bounds = new Vector2Int(workplace.MaxWorkers, workplace.MaxWorkers);
        Console.WriteLine(06);
        if (powerEnabled) bounds = Vector2Int.Min(bounds, powerComponent.EmploymentBounds);
        Console.WriteLine(07);
        if (productEnabled) bounds = Vector2Int.Min(bounds, productComponent.EmploymentBounds);
        Console.WriteLine(08);
        if (ingredientsEnabled) bounds = Vector2Int.Min(bounds, ingredientComponent.EmploymentBounds);
        Console.WriteLine(09);

        // perform employment
        var currentDesiredWorkers = GetCurrentDesiredWorkers();
        if (currentDesiredWorkers < bounds.x)
            IncreaseDesiredWorkers();
        else if (currentDesiredWorkers > bounds.y)
            DecreaseDesiredWorkers();
    }


    private void IncreaseDesiredWorkers() => workplace?.IncreaseDesiredWorkers();

    private void DecreaseDesiredWorkers() => workplace?.DecreaseDesiredWorkers();

    private int GetCurrentDesiredWorkers() => workplace?.DesiredWorkers ?? 0;
}