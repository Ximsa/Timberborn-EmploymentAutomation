
using Bindito.Core;
using System;
using System.Collections.Generic;
using Timberborn.BuildingsBlocking;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.GoodsUI;
using Timberborn.Persistence;
using Timberborn.TickSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using UnityEngine;

namespace EmploymentAutomation
{
    public class EmploymentManagerComponent : TickableComponent, IPersistentEntity
    {
        private GoodDescriber goodDescriber;
        private DistrictResourceCounterService districtResourceCounterService;

        private static readonly ComponentKey EmploymentManagerComponentKey = new ComponentKey("EmploymentManagerComponent");
        private static readonly PropertyKey<bool> OutStockActiveKey = new PropertyKey<bool>("OutStockActive");
        private static readonly PropertyKey<float> OutStockHighKey = new PropertyKey<float>("OutStockHigh");
        private static readonly PropertyKey<float> OutStockLowKey = new PropertyKey<float>("OutStockLow");
        private static readonly PropertyKey<bool> InStockActiveKey = new PropertyKey<bool>("InStockActive");
        private static readonly PropertyKey<float> InStockHighKey = new PropertyKey<float>("InStockHigh");
        private static readonly PropertyKey<float> InStockLowKey = new PropertyKey<float>("InStockLow");

        public bool availible = false;
        public bool outStockAvailible = false;
        public bool inStockAvailible = false;
        public bool outStockActive = false;
        public float outStockHigh = 0.95f;
        public float outStockLow = 0.85f;
        public bool inStockActive = false;
        public float inStockHigh = 0.15f;
        public float inStockLow = 0.05f;
        public string inStockText = "";
        public string outStockText = "";

        [Inject]
        public void InjectDependencies(DistrictResourceCounterService districtResourceCounterService, GoodDescriber goodDescriber)
        {
            this.goodDescriber = goodDescriber;
            this.districtResourceCounterService = districtResourceCounterService;
        }

        public override void Tick()
        {
            Workplace workplace = base.GetComponentFast<Workplace>();
            Manufactory manufactory = base.GetComponentFast<Manufactory>();
            DistrictBuilding districtBuilding = base.GetComponentFast<DistrictBuilding>();
            PausableBuilding pausableBuilding = base.GetComponentFast<PausableBuilding>();
            availible = (pausableBuilding != null) && (districtBuilding != null) && (districtBuilding.InstantDistrict != null) && (workplace != null) && (manufactory != null) && manufactory.HasCurrentRecipe && (manufactory.CurrentRecipe.ProducesProducts || manufactory.CurrentRecipe.ConsumesIngredients);
            if (availible)
            {
                // obtain fillrate of output
                IReadOnlyList<GoodAmount> products = manufactory.CurrentRecipe.Products;
                float productFillrate = 1.0f;
                outStockAvailible = products.Count != 0;
                bool checkOutStock = outStockAvailible && outStockActive;
                if (checkOutStock) 
                {
                    outStockText = products.Count == 0 ? "" : goodDescriber.Describe(products[0].GoodId);
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
                inStockAvailible = ingredients.Count != 0;
                float ingredientFillrate = 1.0f;
                bool checkInStock = inStockAvailible && inStockActive;
                if (checkInStock)
                {
                    inStockText = products.Count == 0 ? "" : goodDescriber.Describe(ingredients[0].GoodId);
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
                // employment triggers
                Tuple<int, int> bounds = GetEmploymentBounds(checkOutStock, checkInStock, productFillrate, ingredientFillrate, workplace);
                int currentDesiredWorkers = GetCurrentDesiredWorkers(pausableBuilding, workplace);
                if (currentDesiredWorkers < bounds.Item1) // understaffed?
                {
                    IncreaseDesiredWorkers(pausableBuilding, workplace);
                }
                else if (currentDesiredWorkers > bounds.Item2) // overstaffed?
                {
                    DecreaseDesiredWorkers(pausableBuilding, workplace);
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
                workplace.IncreaseDesiredWorkers();
            }
        }

        public Tuple<int,int> GetEmploymentBounds(bool checkOutStock, bool checkInStock, float productFillrate, float incredientFillrate, Workplace workplace)
        {
            // determine bounds for output
            int outputLow = workplace.MaxWorkers;
            int outputHigh = workplace.MaxWorkers;
            if (checkOutStock)
            {
                outputLow = workplace.MaxWorkers;
                for (int i = 0; i < workplace.MaxWorkers; i++)
                {
                    float lowThreshold = GetValueBetween(outStockLow, outStockHigh, workplace.MaxWorkers * 2, i);
                    float highThreshold = GetValueBetween(outStockLow, outStockHigh, workplace.MaxWorkers * 2, workplace.MaxWorkers * 2 - i - 1);
                    if (productFillrate > lowThreshold)
                    {
                        outputLow--;
                    }
                    if (productFillrate > highThreshold)
                    {
                        outputHigh--;
                    }
                }
            }
            // determine bounds for input, basicly above in reverse
            int inputLow = workplace.MaxWorkers;
            int inputHigh = workplace.MaxWorkers;
            if (checkInStock)
            {
                inputLow = workplace.MaxWorkers;
                for (int i = 0; i < workplace.MaxWorkers; i++)
                {
                    float lowThreshold = GetValueBetween(inStockLow, inStockHigh, workplace.MaxWorkers * 2, i);
                    float highThreshold = GetValueBetween(inStockLow, inStockHigh, workplace.MaxWorkers * 2, workplace.MaxWorkers * 2 - i - 1);
                    if (incredientFillrate < lowThreshold)
                    {
                        inputHigh--;
                    }
                    if (incredientFillrate < highThreshold)
                    {
                        inputLow--;
                    }
                }
            }
            // clamp to workers allowed from input stock levels
            return new Tuple<int, int>(
                Mathf.Min(outputLow,  inputLow), 
                Mathf.Min(outputHigh, inputHigh));
        }

        private float GetValueBetween(float min, float max, int num_values, int index)
        {
            float diff = max - min;
            float offset = index * diff / (num_values-1);
            return min + offset;
        }

        public void Save(IEntitySaver entitySaver)
        {
            IObjectSaver component = entitySaver.GetComponent(EmploymentManagerComponentKey);
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
