using System;
using System.Linq;
using Bindito.Core;
using Timberborn.GameDistricts;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace EmploymentAutomation;

public class IngredientComponent : TickableComponent, IPersistentEntity
{
    private static readonly ComponentKey EmploymentManagerComponentKey = new("EmploymentManagerIngredientComponent");

    private static readonly PropertyKey<bool> InStockActiveKey = new("InStockActive");
    private static readonly PropertyKey<float> InStockHighKey = new("InStockHigh");
    private static readonly PropertyKey<float> InStockLowKey = new("InStockLow");

    public bool Available { get; private set; }
    public bool InStockActive { get; private set; }
    public float InStockHigh { get; set; } = 0.95f;
    public float InStockLow { get; set; } = 0.75f;

    public Vector2Int EmploymentBounds { get; private set; } = new();

    private DistrictBuilding districtBuilding;
    private DistrictResourceCounterService districtResourceCounterService;
    private Manufactory manufactory;
    private Workplace workplace;

    public void Save(IEntitySaver entitySaver)
    {
        var component = entitySaver.GetComponent(EmploymentManagerComponentKey);
        component.Set(InStockActiveKey, InStockActive);
        component.Set(InStockLowKey, InStockLow);
        component.Set(InStockHighKey, InStockHigh);
    }

    public void Load(IEntityLoader entityLoader)
    {
        try
        {
            var component = entityLoader.GetComponent(EmploymentManagerComponentKey);
            InStockActive = component.Get(InStockActiveKey);
            InStockLow = component.Get(InStockLowKey);
            InStockHigh = component.Get(InStockHighKey);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [Inject]
    public void InjectDependencies(
        DistrictResourceCounterService districtResourceCounterService, EventBus eventBus)
    {
        eventBus.Register(this);
        this.districtResourceCounterService = districtResourceCounterService;
        UpdateComponents();
    }

    private void UpdateComponents()
    {
        try
        {
            workplace = GetComponent<Workplace>();
            manufactory = GetComponent<Manufactory>();
            districtBuilding = GetComponent<DistrictBuilding>();
            Available = true;
        }
        catch (Exception)
        {
            Available = false;
        }
    }

    public override void Tick()
    {
        if (!Available || !manufactory.HasCurrentRecipe || !manufactory.CurrentRecipe.ConsumesIngredients) return;

        var ingredients = manufactory.CurrentRecipe.Ingredients;
        var ingredientFillrate = Enumerable.Aggregate(
            ingredients,
            1.0f,
            (current, ingredient) => Mathf.Min(
                current,
                districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, ingredient.Id)));

        // employment trigger bounds
        EmploymentBounds = GetEmploymentBoundsIngredient(ingredientFillrate);
    }

    private Vector2Int GetEmploymentBoundsIngredient(float fillrate)
    {
        var bounds = new Vector2Int(workplace.MaxWorkers, 0);
        var offset = (InStockHigh - InStockLow) / (workplace.MaxWorkers * 2 - 1);
        var low = InStockLow;
        var high = InStockHigh;
        for (var i = 0; i < workplace.MaxWorkers; i++)
        {
            bounds.y += Convert.ToInt32(fillrate > low); // fillrate above low threshold? add one maximum worker
            bounds.x -= Convert.ToInt32(fillrate <
                                        high); // fillrate below high threshold? remove one minimum worker
            low += offset;
            high -= offset;
        }

        return bounds;
    }
}