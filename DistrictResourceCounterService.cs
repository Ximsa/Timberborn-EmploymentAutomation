
using Bindito.Core;
using System.Collections.Generic;
using Timberborn.Common;
using Timberborn.GameDistricts;
using Timberborn.Goods;
using Timberborn.InventorySystem;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using UnityEngine;

namespace EmploymentAutomation
{
    public class DistrictResourceCounterService : ITickableSingleton, IPostLoadableSingleton
    {
        private InventoryService inventoryService;
        private EventBus eventBus;

        private readonly Dictionary<DistrictCenter, Dictionary<string, int[]>> stockCounter = new Dictionary<DistrictCenter, Dictionary<string, int[]>>();

        [Inject]
        public void InjectDependencies(InventoryService inventoryService, EventBus eventBus)
        {
            this.inventoryService = inventoryService;
            this.eventBus = eventBus;
        }
        public void PostLoad()
        {
            eventBus.Register(this);
            UpdateResources();
        }

        public void Tick()
        {
            UpdateResources();
        }

        [OnEvent]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Nicht verwendete Parameter entfernen")]
        public void OnNewGameInitialized(NewGameInitializedEvent newGameInitializedEvent)
        {
            UpdateResources();
        }
        /*
        [OnEvent]
        public void OnSelectableObjectSelected(SelectableObjectSelectedEvent selectableObjectSelectedEvent)
        {
            
            SelectableObject o = selectableObjectSelectedEvent.SelectableObject;
            if(o)
            {
                Console.WriteLine("\nComponents of Object:");
                foreach (var component in o.AllComponents)
                {
                    Console.WriteLine(component.ToString());
                }
                Console.WriteLine("-------------");
            }
        }*/

        public float GetFillRate(DistrictCenter districtCenter, string goodId)
        {
            float result;
            if(stockCounter.TryGetValue(districtCenter, out Dictionary<string, int[]> districtStockCounter))
            {
                if(districtStockCounter.TryGetValue(goodId, out int[] goodStats))
                {
                    if(goodStats[0] == 0)
                    {
                        result = 0f;
                    } else if(goodStats[1] == 0)
                    {
                        result = 1f;
                    } else
                    {
                        result = Mathf.Clamp01((float)goodStats[0] / (float)goodStats[1]);
                    }
                } else
                {
                    result = 0f;
                }
            } else
            {
                result = 0f;
            }
            return result;
        }

        private void UpdateResources()
        {
            ResetCounter();
            foreach (Inventory inventory in inventoryService.PublicOutputInventories)
            {
                AddInventoryToCounter(inventory);
            }
        }
        private void ResetCounter()
        {
            /*Console.WriteLine("Resouce Counter:");*/
            foreach(var goods in stockCounter.Values)
            {
                foreach(var counts in goods.Values)
                {
                    /*Console.WriteLine(new Tuple<int, int>(counts[0], counts[1]));*/
                    counts[0] = 0;
                    counts[1] = 0;
                }
            }
            /*Console.WriteLine("----------------------------------");*/
        }
        private void AddInventoryToCounter(Inventory inventory)
        {
            // Add district
            DistrictCenter districtcenter = inventory.GetComponentFast<DistrictBuilding>().InstantDistrict;
            if(districtcenter) // is inventory connected to a district?
            {
                if (!stockCounter.ContainsKey(districtcenter))
                {
                    stockCounter.Add(districtcenter, new Dictionary<string, int[]>());
                }
                // Count capacities
                List<GoodAmount> capacityCache = new List<GoodAmount>();
                inventory.GetCapacity(capacityCache);
                foreach (GoodAmount good in capacityCache)
                {
                    if (inventory.Gives(good.GoodId))
                    {
                        AddAmountToCounter(districtcenter, good.GoodId, good.Amount, 1);
                    }
                }
                // Count stock
                foreach (GoodAmount good in inventory.Stock)
                {
                    if (inventory.Gives(good.GoodId))
                    {
                        AddAmountToCounter(districtcenter, good.GoodId, good.Amount, 0);
                    }
                } 
            }
        }
        private void AddAmountToCounter(DistrictCenter districtcenter, string goodId, int amount, int index)
        {
            if (stockCounter.TryGetValue(districtcenter, out Dictionary<string, int[]> districtStockCounter))
            {
                if (districtStockCounter.TryGetValue(goodId, out int[] goodStats))
                {
                    goodStats[index] += amount;
                }
                else
                {
                    goodStats = new int[2];
                    goodStats[index] = amount;
                    districtStockCounter.Add(goodId, goodStats);
                }
            }
        }
    }
}
