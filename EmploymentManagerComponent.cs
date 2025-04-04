using System;
using System.Collections.Immutable;
using System.Linq;
using Bindito.Core;
using Timberborn.BuildingsBlocking;
using Timberborn.GameDistricts;
using Timberborn.MechanicalSystem;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace EmploymentAutomation
{
    public class EmploymentManagerComponent : TickableComponent, IPersistentEntity
    {
        private static readonly ComponentKey EmploymentManagerComponentKey =
            new ComponentKey("EmploymentManagerComponent");

        private static readonly PropertyKey<bool> PowerActiveKey = new PropertyKey<bool>("PowerActive");
        private static readonly PropertyKey<float> PowerHighKey = new PropertyKey<float>("PowerHigh");
        private static readonly PropertyKey<float> PowerLowKey = new PropertyKey<float>("PowerLow");
        private static readonly PropertyKey<bool> OutStockActiveKey = new PropertyKey<bool>("OutStockActive");
        private static readonly PropertyKey<float> OutStockHighKey = new PropertyKey<float>("OutStockHigh");
        private static readonly PropertyKey<float> OutStockLowKey = new PropertyKey<float>("OutStockLow");
        private static readonly PropertyKey<bool> InStockActiveKey = new PropertyKey<bool>("InStockActive");
        private static readonly PropertyKey<float> InStockHighKey = new PropertyKey<float>("InStockHigh");
        private static readonly PropertyKey<float> InStockLowKey = new PropertyKey<float>("InStockLow");

        public bool available;
        public bool powerAvailable;
        public bool outStockAvailable;
        public bool inStockAvailable;

        public bool powerActive;
        public float powerHigh = 0.75f;
        public float powerLow = 0.50f;
        public bool outStockActive;
        public float outStockHigh = 0.95f;
        public float outStockLow = 0.85f;
        public bool inStockActive;
        public float inStockHigh = 0.15f;
        public float inStockLow = 0.05f;
        private BlockableBuilding blockableBuilding;
        private DistrictBuilding districtBuilding;
        private DistrictResourceCounterService districtResourceCounterService;
        private Manufactory manufactory;
        private MechanicalNode mechanicalNode;
        private MechanicalNodeSpec mechanicalNodeSpecification;

        private Workplace workplace;

        public void Awake()
        {
            UpdateComponents();
        }

        public void Save(IEntitySaver entitySaver)
        {
            var component = entitySaver.GetComponent(EmploymentManagerComponentKey);
            component.Set(PowerActiveKey, powerActive);
            component.Set(PowerLowKey, powerLow);
            component.Set(PowerHighKey, powerHigh);
            component.Set(OutStockActiveKey, outStockActive);
            component.Set(OutStockLowKey, outStockLow);
            component.Set(OutStockHighKey, outStockHigh);
            component.Set(InStockActiveKey, inStockActive);
            component.Set(InStockLowKey, inStockLow);
            component.Set(InStockHighKey, inStockHigh);
        }

        public void Load(IEntityLoader entityLoader)
        {
            try
            {
                var component = entityLoader.GetComponent(EmploymentManagerComponentKey);
                powerActive = component.Get(PowerActiveKey);
                outStockActive = component.Get(OutStockActiveKey);
                outStockLow = component.Get(OutStockLowKey);
                outStockHigh = component.Get(OutStockHighKey);
                inStockActive = component.Get(InStockActiveKey);
                inStockLow = component.Get(InStockLowKey);
                inStockHigh = component.Get(InStockHighKey);
                powerLow = component.Get(PowerLowKey);
                powerHigh = component.Get(PowerHighKey);
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

        public void UpdateComponents()
        {
            workplace = GetComponentFast<Workplace>();
            manufactory = GetComponentFast<Manufactory>();
            districtBuilding = GetComponentFast<DistrictBuilding>();
            blockableBuilding = GetComponentFast<BlockableBuilding>();
            if (AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes())
                .Any(x => x.Namespace is "IgorZ.SmartPower.Core"))
                return;

            mechanicalNode = GetComponentFast<MechanicalNode>();
            mechanicalNodeSpecification = GetComponentFast<MechanicalNodeSpec>();
        }

        public override void Tick()
        {
            available = blockableBuilding != null &&
                        districtBuilding != null &&
                        districtBuilding.InstantDistrict != null &&
                        workplace != null &&
                        manufactory != null &&
                        manufactory.HasCurrentRecipe &&
                        (manufactory.CurrentRecipe.ProducesProducts || manufactory.CurrentRecipe.ConsumesIngredients);
            if (available)
            {
                powerAvailable = mechanicalNodeSpecification != null && mechanicalNode != null &&
                                 mechanicalNode.Graph != null && mechanicalNode.IsConsumer;
                outStockAvailable = !manufactory.CurrentRecipe.Products.IsEmpty;
                inStockAvailable = !manufactory.CurrentRecipe.Ingredients.IsEmpty;
            }

            var checkOutStock = outStockAvailable && outStockActive;
            var checkInStock = inStockAvailable && inStockActive;
            var checkPower = powerAvailable && powerActive;

            if (!available || (!checkOutStock && !checkInStock && !checkPower)) return;
            // obtain power availability. USes Battery charge when availible. Falls back to network efficiency when no battery is attached
            var powerMeter = 1.0f;
            if (checkPower)
            {
                var batteries = mechanicalNode.Graph!.BatteryControllers
                    .Where(x => x.Operational)
                    .ToImmutableArray();
                if (batteries.IsEmpty)
                {
                    // need to recalculate efficiency to account for own activation
                    var currentPower = mechanicalNode.Graph!.CurrentPower;
                    powerMeter = Mathf.Min(
                        (currentPower.PowerSupply + currentPower.BatteryPower) /
                        (currentPower.PowerDemand + (GetCurrentDesiredWorkers() == 0
                            ? mechanicalNodeSpecification.PowerInput
                            : 0f)),
                        1f);
                }
                else
                {
                    // calculate battery fill level
                    powerMeter = batteries.Select(x => x.Charge).Sum() /
                                 batteries.Select(x => x.Capacity).Sum();
                }
            }

            // obtain fillrate of output
            var products = manufactory.CurrentRecipe.Products;
            var productFillrate = 1.0f;
            if (checkOutStock)
                productFillrate = Enumerable.Aggregate(
                    products,
                    productFillrate,
                    (current, product) =>
                        Mathf.Min(
                            current,
                            districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, product.Id)));

            // obtain fillrate of input
            var ingredients = manufactory.CurrentRecipe.Ingredients;
            var ingredientFillrate = 1.0f;
            if (checkInStock)
                ingredientFillrate = Enumerable.Aggregate(
                    ingredients,
                    ingredientFillrate,
                    (current, ingredient) => Mathf.Min(
                        current,
                        districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, ingredient.Id)));

            // employment trigger bounds
            var bounds = new Vector2Int(workplace.MaxWorkers, workplace.MaxWorkers);
            if (checkPower) bounds = Vector2Int.Min(bounds, GetEmploymentBoundsPower(powerMeter));

            if (checkOutStock) bounds = Vector2Int.Min(bounds, GetEmploymentBoundsProduct(productFillrate));

            if (checkInStock) bounds = Vector2Int.Min(bounds, GetEmploymentBoundsIngredient(ingredientFillrate));

            // perform employment
            var currentDesiredWorkers = GetCurrentDesiredWorkers();
            if (currentDesiredWorkers < bounds.x)
                IncreaseDesiredWorkers();
            else if (currentDesiredWorkers > bounds.y) DecreaseDesiredWorkers();
        }

        public void IncreaseDesiredWorkers()
        {
            if (blockableBuilding.IsUnblocked)
                workplace.IncreaseDesiredWorkers();
            else
                blockableBuilding.Unblock(this);
        }

        public void DecreaseDesiredWorkers()
        {
            if (workplace.DesiredWorkers <= 1)
                blockableBuilding.Block(this);
            else
                workplace.DecreaseDesiredWorkers();
        }

        private int GetCurrentDesiredWorkers()
        {
            return blockableBuilding.IsUnblocked ? workplace.DesiredWorkers : 0;
        }

        private Vector2Int GetEmploymentBoundsPower(float powerMeter)
        {
            return new Vector2Int(
                powerMeter < powerHigh ? 0 : workplace.MaxWorkers, // min
                powerMeter < powerLow ? 0 : workplace.MaxWorkers);
            // max
        }

        private Vector2Int GetEmploymentBoundsProduct(float fillrate)
        {
            var bounds = new Vector2Int(workplace.MaxWorkers, 0);
            var offset = (outStockHigh - outStockLow) / (workplace.MaxWorkers * 2 - 1);
            var low = outStockLow;
            var high = outStockHigh;
            for (var i = 0; i < workplace.MaxWorkers; i++)
            {
                bounds.x -= Convert.ToInt32(fillrate > low); // fillrate above low threshold? remove one minimum worker
                bounds.y += Convert.ToInt32(fillrate < high); // fillrate below high threshold? add one maximum worker
                low += offset;
                high -= offset;
            }

            return bounds;
        }

        private Vector2Int GetEmploymentBoundsIngredient(float fillrate)
        {
            var bounds = new Vector2Int(workplace.MaxWorkers, 0);
            var offset = (inStockHigh - inStockLow) / (workplace.MaxWorkers * 2 - 1);
            var low = inStockLow;
            var high = inStockHigh;
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
}