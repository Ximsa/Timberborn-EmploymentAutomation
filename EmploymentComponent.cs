using System;
using Bindito.Core;
using JetBrains.Annotations;
using Timberborn.Buildings;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.WorkSystem;
using UnityEngine;

namespace EmploymentAutomation;

public class EmploymentComponent : TickableComponent
{
    [CanBeNull] private PausableBuilding pausableBuilding;
    [CanBeNull] private Workplace workplace;
    [CanBeNull] private PowerComponent powerComponent;
    [CanBeNull] private ProductComponent productComponent;
    [CanBeNull] private IngredientComponent ingredientComponent;

    [Inject]
    public void InjectDependencies(
        EventBus eventBus)
    {
        eventBus.Register(this);
        UpdateComponents();
    }

    private void UpdateComponents()
    {
        TryGetComponent(out pausableBuilding);
        TryGetComponent(out workplace);
        TryGetComponent(out powerComponent);
        TryGetComponent(out productComponent);
        TryGetComponent(out ingredientComponent);
    }

    public override void Tick()
    {
        
        var ingredientsEnabled = ingredientComponent is { Available: true };
        var productEnabled = productComponent is { Available: true };
        var powerEnabled = powerComponent is { Available: true };
        var hasToTick = workplace is not null && pausableBuilding is not null &&
                        (ingredientsEnabled || productEnabled || powerEnabled);
        Console.WriteLine(hasToTick);
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
        if (pausableBuilding is null || workplace is null) return;
        if (pausableBuilding.Paused)
            pausableBuilding.Resume();
        else
            workplace.IncreaseDesiredWorkers();
    }

    private void DecreaseDesiredWorkers()
    {
        if (pausableBuilding is null || workplace is null) return;
        if (workplace.DesiredWorkers <= 1)
            pausableBuilding.Pause();
        else
            workplace.DecreaseDesiredWorkers();
    }

    private int GetCurrentDesiredWorkers()
    {
        return workplace?.DesiredWorkers ?? 0;
    }
}