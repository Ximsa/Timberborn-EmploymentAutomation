
using Bindito.Core;
using System;
using System.Collections.Generic;
using Timberborn.BuildingsBlocking;
using Timberborn.ConstructibleSystem;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.GoodsUI;
using Timberborn.MechanicalSystem;
using Timberborn.Persistence;
using Timberborn.SelectionSystem;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.TimeSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using UnityEngine;

namespace EmploymentAutomation
{
    public class EmploymentManagerComponent : TickableComponent, IPersistentEntity
    {
        private EventBus eventBus;
        private GoodDescriber goodDescriber;
        private DistrictResourceCounterService districtResourceCounterService;

        private Workplace workplace;
        private Manufactory manufactory;
        private DistrictBuilding districtBuilding;
        private PausableBuilding pausableBuilding;
        private MechanicalNode mechanicalNode;

        private static readonly ComponentKey EmploymentManagerComponentKey = new ComponentKey("EmploymentManagerComponent");
        private static readonly PropertyKey<bool> PowerActiveKey = new PropertyKey<bool>("PowerActive");
        private static readonly PropertyKey<bool> OutStockActiveKey = new PropertyKey<bool>("OutStockActive");
        private static readonly PropertyKey<float> OutStockHighKey = new PropertyKey<float>("OutStockHigh");
        private static readonly PropertyKey<float> OutStockLowKey = new PropertyKey<float>("OutStockLow");
        private static readonly PropertyKey<bool> InStockActiveKey = new PropertyKey<bool>("InStockActive");
        private static readonly PropertyKey<float> InStockHighKey = new PropertyKey<float>("InStockHigh");
        private static readonly PropertyKey<float> InStockLowKey = new PropertyKey<float>("InStockLow");

        public bool availible = false;
        public bool powerAvailible = false;
        public bool outStockAvailible = false;
        public bool inStockAvailible = false;

        public bool powerActive = false;
        public bool outStockActive = false;
        public float outStockHigh = 0.95f;
        public float outStockLow = 0.85f;
        public bool inStockActive = false;
        public float inStockHigh = 0.15f;
        public float inStockLow = 0.05f;
        public string inStockText = "Ingredient";
        public string outStockText = "Product";

        [Inject]
        public void InjectDependencies(DistrictResourceCounterService districtResourceCounterService, GoodDescriber goodDescriber, EventBus eventBus)
        {
            eventBus.Register(this);
            this.eventBus = eventBus;
            this.goodDescriber = goodDescriber;
            this.districtResourceCounterService = districtResourceCounterService;
            UpdateComponents();
        }
        // update components here and there
        [OnEvent] 
        public void OnSelectableObjectSelected(SelectableObjectSelectedEvent selectableObjectSelectedEvent)
        {
            UpdateComponents();
        }
        [OnEvent]
        public void OnDaytimeStartEvent(DaytimeStartEvent daytimeStartEvent)
        {
            UpdateComponents();
        }

        public void UpdateComponents()
        {
            workplace = base.GetComponentFast<Workplace>();
            manufactory = base.GetComponentFast<Manufactory>();
            districtBuilding = base.GetComponentFast<DistrictBuilding>();
            pausableBuilding = base.GetComponentFast<PausableBuilding>();
            mechanicalNode = base.GetComponentFast<MechanicalNode>();
        }

        public override void Tick()
        {
            availible = (pausableBuilding != null) && (districtBuilding != null) && (districtBuilding.InstantDistrict != null) && (workplace != null) && (manufactory != null) && manufactory.HasCurrentRecipe && (manufactory.CurrentRecipe.ProducesProducts || manufactory.CurrentRecipe.ConsumesIngredients);
            if(availible)
            {
                powerAvailible = mechanicalNode != null && mechanicalNode.IsConsumer;
                outStockAvailible = manufactory.CurrentRecipe.Products.Count != 0;
                inStockAvailible = manufactory.CurrentRecipe.Ingredients.Count != 0;
            }
            bool checkOutStock = outStockAvailible && outStockActive;
            bool checkInStock = inStockAvailible && inStockActive;
            bool checkPower = powerActive && powerAvailible;

            if (availible && (checkOutStock || checkInStock || checkPower))
            {
                // obtain fillrate of output
                IReadOnlyList<GoodAmount> products = manufactory.CurrentRecipe.Products;
                float productFillrate = 1.0f;
                if (checkOutStock) 
                {
                    outStockText = products.Count == 0 ? outStockText : goodDescriber.Describe(products[0].GoodId);
                    foreach (GoodAmount product in products)
                    {
                        float fillLevel = districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, product.GoodId);
                        if (fillLevel < productFillrate)
                        {
                            outStockText = goodDescriber.Describe(product.GoodId);
                            productFillrate = fillLevel;
                        }
                    } 
                }
                // obtain fillrate of input
                IReadOnlyList<GoodAmount> ingredients = manufactory.CurrentRecipe.Ingredients;
                float ingredientFillrate = 1.0f;
                if (checkInStock)
                {
                    inStockText = products.Count == 0 ? inStockText : goodDescriber.Describe(ingredients[0].GoodId);
                    foreach (GoodAmount incredient in ingredients)
                    {
                        float fillLevel = districtResourceCounterService.GetFillRate(districtBuilding.InstantDistrict, incredient.GoodId);
                        if (fillLevel < ingredientFillrate)
                        {
                            inStockText = goodDescriber.Describe(incredient.GoodId);
                            ingredientFillrate = fillLevel;
                        }
                    }
                }
                // employment trigger bounds
                Vector2Int bounds = new Vector2Int(workplace.MaxWorkers, workplace.MaxWorkers);
                if (checkPower)
                {
                    bounds = Vector2Int.Min(bounds, GetEmploymentBoundsPower());
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
                int currentDesiredWorkers = this.GetCurrentDesiredWorkers(this.pausableBuilding, this.workplace);
                if (currentDesiredWorkers < bounds.x)
                {
                    this.IncreaseDesiredWorkers(this.pausableBuilding, this.workplace);
                }
                else if (currentDesiredWorkers > bounds.y)
                {
                    this.DecreaseDesiredWorkers(this.pausableBuilding, this.workplace);
                }
            }
        }

        public int GetCurrentDesiredWorkers(PausableBuilding pausableBuilding, Workplace workplace)
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

        public void IncreaseDesiredWorkers(PausableBuilding pausableBuilding, Workplace workplace)
        {
            if (pausableBuilding.Paused)
            {
                pausableBuilding.Resume();
            } else
            {
                workplace.IncreaseDesiredWorkers();
            }
        }
        public void DecreaseDesiredWorkers(PausableBuilding pausableBuilding, Workplace workplace)
        {
            if(workplace.DesiredWorkers <= 1)
            {
                pausableBuilding.Pause();
            } else
            {
                workplace.DecreaseDesiredWorkers();
            }
        }

        public Vector2Int GetEmploymentBoundsPower()
        {
            Vector2Int bounds = new Vector2Int(0, 0);
            if (!powerAvailible || (powerAvailible && powerActive && mechanicalNode.Powered)) // do we have no power requirements or are powered?
            {
                bounds.x = workplace.MaxWorkers;
                bounds.y = workplace.MaxWorkers;
            }
            return bounds;
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
            } catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
