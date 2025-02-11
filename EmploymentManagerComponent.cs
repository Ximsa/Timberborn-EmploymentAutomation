
using Bindito.Core;
using System;
using System.Collections.Generic;
using Timberborn.BuildingsBlocking;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.MechanicalSystem;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using UnityEngine;

namespace EmploymentAutomation
{
    public class EmploymentManagerComponent : TickableComponent, IPersistentEntity
    {
        private DistrictResourceCounterService districtResourceCounterService;

        private Workplace workplace;
        private Manufactory manufactory;
        private DistrictBuilding districtBuilding;
        private PausableBuilding pausableBuilding;
        private MechanicalNode mechanicalNode;
        private MechanicalNodeSpecification mechanicalNodeSpecification;

        private static readonly ComponentKey EmploymentManagerComponentKey = new ComponentKey("EmploymentManagerComponent");
        private static readonly PropertyKey<bool> PowerActiveKey = new PropertyKey<bool>("PowerActive");
        private static readonly PropertyKey<float> PowerHighKey = new PropertyKey<float>("PowerHigh");
        private static readonly PropertyKey<float> PowerLowKey = new PropertyKey<float>("PowerLow");
        private static readonly PropertyKey<bool> OutStockActiveKey = new PropertyKey<bool>("OutStockActive");
        private static readonly PropertyKey<float> OutStockHighKey = new PropertyKey<float>("OutStockHigh");
        private static readonly PropertyKey<float> OutStockLowKey = new PropertyKey<float>("OutStockLow");
        private static readonly PropertyKey<bool> InStockActiveKey = new PropertyKey<bool>("InStockActive");
        private static readonly PropertyKey<float> InStockHighKey = new PropertyKey<float>("InStockHigh");
        private static readonly PropertyKey<float> InStockLowKey = new PropertyKey<float>("InStockLow");

        public bool available = false;
        public bool powerAvailible = false;
        public bool outStockAvailible = false;
        public bool inStockAvailible = false;

        public bool powerActive = false;
        public float powerHigh = 0.75f;
        public float powerLow = 0.50f;
        public bool outStockActive = false;
        public float outStockHigh = 0.95f;
        public float outStockLow = 0.85f;
        public bool inStockActive = false;
        public float inStockHigh = 0.15f;
        public float inStockLow = 0.05f;

        [Inject]
        public void InjectDependencies(DistrictResourceCounterService districtResourceCounterService, EventBus eventBus)
        {
            eventBus.Register(this);
            this.districtResourceCounterService = districtResourceCounterService;
            UpdateComponents();
        }

        public void Awake()
        {
            UpdateComponents();
        }

        public void UpdateComponents()
        {
            workplace = base.GetComponentFast<Workplace>();
            manufactory = base.GetComponentFast<Manufactory>();
            districtBuilding = base.GetComponentFast<DistrictBuilding>();
            pausableBuilding = base.GetComponentFast<PausableBuilding>();
            if (Type.GetType("IgorZ.SmartPower.Core.Configurator") == null) // smartpower not detected, provide own power management
            {
                mechanicalNode = base.GetComponentFast<MechanicalNode>();
                mechanicalNodeSpecification = base.GetComponentFast<MechanicalNodeSpecification>();
            }
        }

        public override void Tick()
        {
            available = (pausableBuilding != null) && (districtBuilding != null) && (districtBuilding.InstantDistrict != null) && (workplace != null) && (manufactory != null) && manufactory.HasCurrentRecipe && (manufactory.CurrentRecipe.ProducesProducts || manufactory.CurrentRecipe.ConsumesIngredients);
            if(available)
            {
                powerAvailible = mechanicalNodeSpecification != null && mechanicalNode != null && mechanicalNode.Graph != null && mechanicalNode.IsConsumer;
                outStockAvailible = manufactory.CurrentRecipe.Products.Count != 0;
                inStockAvailible = manufactory.CurrentRecipe.Ingredients.Count != 0;
            }
            bool checkOutStock = outStockAvailible && outStockActive;
            bool checkInStock = inStockAvailible && inStockActive;
            bool checkPower = powerActive && powerAvailible;

            if (available && (checkOutStock || checkInStock || checkPower))
            {
                // obtain power availability
                float powerMeter = 1.0f;
                if (checkPower)
                {
                    
                    MechanicalGraphPower currentPower = mechanicalNode.Graph.CurrentPower;
                    powerMeter = Mathf.Min(
                        (currentPower.PowerSupply + currentPower.BatteryPower)/
                        (currentPower.PowerDemand + (GetCurrentDesiredWorkers() == 0 ? mechanicalNodeSpecification.PowerInput : 0f)),
                        1f);
                }
                // obtain fillrate of output
                IReadOnlyList<GoodAmount> products = manufactory.CurrentRecipe.Products;
                float productFillrate = 1.0f;
                if (checkOutStock) 
                {
                    foreach (GoodAmount product in products)
                    {
                        productFillrate = Mathf.Min(productFillrate, districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, product.GoodId));
                    } 
                }
                // obtain fillrate of input
                IReadOnlyList<GoodAmount> ingredients = manufactory.CurrentRecipe.Ingredients;
                float ingredientFillrate = 1.0f;
                if (checkInStock)
                {
                    foreach (GoodAmount incredient in ingredients)
                    {
                        ingredientFillrate = Mathf.Min(ingredientFillrate, districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, incredient.GoodId));
                    }
                }
                // employment trigger bounds
                Vector2Int bounds = new Vector2Int(workplace.MaxWorkers, workplace.MaxWorkers);
                if (checkPower)
                {
                    bounds = Vector2Int.Min(bounds, GetEmploymentBoundsPower(powerMeter));
                }
                if (checkOutStock)
                {
                    bounds = Vector2Int.Min(bounds, GetEmploymentBoundsProduct(productFillrate));
                }
                if (checkInStock)
                {
                    bounds = Vector2Int.Min(bounds, GetEmploymentBoundsIngredient(ingredientFillrate));
                }
                // perform employment
                int currentDesiredWorkers = this.GetCurrentDesiredWorkers();
                if (currentDesiredWorkers < bounds.x)
                {
                    this.IncreaseDesiredWorkers();
                }
                else if (currentDesiredWorkers > bounds.y)
                {
                    this.DecreaseDesiredWorkers();
                }
            }
        }

        public int GetCurrentDesiredWorkers()
        {
            if (pausableBuilding.Paused)
            {
                return 0;
            }
            else
            {
                return workplace.DesiredWorkers;
            }
        }

        public void IncreaseDesiredWorkers()
        {
            if (pausableBuilding.Paused)
            {
                pausableBuilding.Resume();
            }
            else
            {
                workplace.IncreaseDesiredWorkers();
            }
        }
        public void DecreaseDesiredWorkers()
        {
            if (workplace.DesiredWorkers <= 1)
            {
                pausableBuilding.Pause();
            }
            else
            {
                workplace.DecreaseDesiredWorkers();
            }
        }

        public Vector2Int GetEmploymentBoundsPower(float powerMeter)
        {
            return new Vector2Int(
                powerMeter < powerHigh ? 0 : workplace.MaxWorkers, // min
                powerMeter < powerLow ? 0 : workplace.MaxWorkers); // max
        }

        public Vector2Int GetEmploymentBoundsProduct(float fillrate)
        {
            Vector2Int bounds = new Vector2Int(workplace.MaxWorkers, 0);
            float offset = (outStockHigh - outStockLow) / (workplace.MaxWorkers*2-1);
            float low = outStockLow;
            float high = outStockHigh;
            for (int i = 0; i < workplace.MaxWorkers; i++)
            {
                bounds.x -= Convert.ToInt32(fillrate > low); // fillrate above low threshold? remove one minimum worker
                bounds.y += Convert.ToInt32(fillrate < high); // fillrate below high threshold? add one maximum worker
                low += offset;
                high -= offset;
            }
            return bounds;
        }

        public Vector2Int GetEmploymentBoundsIngredient(float fillrate)
        {
            Vector2Int bounds = new Vector2Int(workplace.MaxWorkers, 0);
            float offset = (inStockHigh - inStockLow) / (workplace.MaxWorkers * 2 - 1);
            float low = inStockLow;
            float high = inStockHigh;
            for (int i = 0; i < workplace.MaxWorkers; i++)
            {
                bounds.y += Convert.ToInt32(fillrate > low); // fillrate above low threshold? add one maximum worker
                bounds.x -= Convert.ToInt32(fillrate < high); // fillrate below high threshold? remove one minimum worker
                low += offset;
                high -= offset;
            }
            return bounds;
        }

        public void Save(IEntitySaver entitySaver)
        {
            IObjectSaver component = entitySaver.GetComponent(EmploymentManagerComponentKey);
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
                IObjectLoader component = entityLoader.GetComponent(EmploymentManagerComponentKey);
                powerActive = component.Get(PowerActiveKey);
                outStockActive = component.Get(OutStockActiveKey);
                outStockLow = component.Get(OutStockLowKey);
                outStockHigh = component.Get(OutStockHighKey);
                inStockActive = component.Get(InStockActiveKey);
                inStockLow = component.Get(InStockLowKey);
                inStockHigh = component.Get(InStockHighKey);
                powerLow = component.Get(PowerLowKey);
                powerHigh = component.Get(PowerHighKey);
            } catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
