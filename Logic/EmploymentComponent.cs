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
    private Workplace workplace;
    private PowerComponent powerComponent;
    private ProductComponent productComponent;
    private IngredientComponent ingredientComponent;
    private bool componentsAreDirty = true;

    [Inject]
    public void InjectDependencies(
        EventBus eventBus)
    {
        eventBus.Register(this);
    }

    private void UpdateComponents()
    {
        workplace = GetComponent<Workplace>();
        powerComponent = GetComponent<PowerComponent>();
        productComponent = GetComponent<ProductComponent>();
        ingredientComponent = GetComponent<IngredientComponent>();
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
        if (!hasToTick) return;
        
        // employment trigger bounds
        var bounds = new Vector2Int(workplace.MaxWorkers, workplace.MaxWorkers);
        if (powerEnabled) bounds = Vector2Int.Min(bounds, powerComponent.EmploymentBounds);
        if (productEnabled) bounds = Vector2Int.Min(bounds, productComponent.EmploymentBounds);
        if (ingredientsEnabled) bounds = Vector2Int.Min(bounds, ingredientComponent.EmploymentBounds);

        // perform employment
        var currentDesiredWorkers = GetCurrentDesiredWorkers();
        if (currentDesiredWorkers < bounds.x)
            IncreaseDesiredWorkers();
        else if (currentDesiredWorkers > bounds.y)
            DecreaseDesiredWorkers();
    }


    private void IncreaseDesiredWorkers()
    {
        Console.WriteLine("Increasing Workers for "+ workplace.Name);
        workplace.IncreaseDesiredWorkers();
    }

    private void DecreaseDesiredWorkers()
    {
        Console.WriteLine("Decreasing Workers for "+ workplace.Name);
        workplace.DecreaseDesiredWorkers();
    }

    private int GetCurrentDesiredWorkers() => workplace?.DesiredWorkers ?? 0;
}