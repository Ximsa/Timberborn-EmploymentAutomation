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

public class ProductComponent : TickableComponent, IPersistentEntity
{
    private static readonly ComponentKey EmploymentManagerComponentKey = new("EmploymentManagerProductComponent");

    private static readonly PropertyKey<bool> OutStockActiveKey = new("OutStockActive");
    private static readonly PropertyKey<float> OutStockHighKey = new("OutStockHigh");
    private static readonly PropertyKey<float> OutStockLowKey = new("OutStockLow");

    public bool Available { get; private set; }
    public bool OutStockActive { get; private set; }
    public float OutStockHigh { get; set; } = 0.95f;
    public float OutStockLow { get; set; } = 0.75f;

    public Vector2Int EmploymentBounds { get; private set; } = new();

    private DistrictBuilding districtBuilding;
    private DistrictResourceCounterService districtResourceCounterService;
    private Manufactory manufactory;
    private Workplace workplace;

    public void Save(IEntitySaver entitySaver)
    {
        var component = entitySaver.GetComponent(EmploymentManagerComponentKey);
        component.Set(OutStockActiveKey, OutStockActive);
        component.Set(OutStockLowKey, OutStockLow);
        component.Set(OutStockHighKey, OutStockHigh);
    }

    public void Load(IEntityLoader entityLoader)
    {
        try
        {
            var component = entityLoader.GetComponent(EmploymentManagerComponentKey);
            OutStockActive = component.Get(OutStockActiveKey);
            OutStockLow = component.Get(OutStockLowKey);
            OutStockHigh = component.Get(OutStockHighKey);
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
        catch (Exception _)
        {
            Available = false;
        }
    }

    public override void Tick()
    {
        if (!Available || !manufactory.HasCurrentRecipe || !manufactory.CurrentRecipe.ProducesProducts) return;

        // obtain fillrate of output
        var products = manufactory.CurrentRecipe.Products;
        var productFillrate = Enumerable.Aggregate(
            products,
            1.0f,
            (current, product) =>
                Mathf.Min(
                    current,
                    districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, product.Id)));

        // employment trigger bounds
        EmploymentBounds = GetEmploymentBoundsProduct(productFillrate);
    }

    private Vector2Int GetEmploymentBoundsProduct(float fillrate)
    {
        var bounds = new Vector2Int(workplace.MaxWorkers, 0);
        var offset = (OutStockHigh - OutStockLow) / (workplace.MaxWorkers * 2 - 1);
        var low = OutStockLow;
        var high = OutStockHigh;
        for (var i = 0; i < workplace.MaxWorkers; i++)
        {
            bounds.x -= Convert.ToInt32(fillrate > low); // fillrate above low threshold? remove one minimum worker
            bounds.y += Convert.ToInt32(fillrate < high); // fillrate below high threshold? add one maximum worker
            low += offset;
            high -= offset;
        }

        return bounds;
    }
}