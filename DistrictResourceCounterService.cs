using System.Collections.Generic;
using System.Linq;
using Bindito.Core;
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
        private readonly Dictionary<DistrictCenter, Dictionary<string, int[]>> stockCounter =
            new Dictionary<DistrictCenter, Dictionary<string, int[]>>();

        private EventBus eventBus;
        private InventoryService inventoryService;

        public void PostLoad()
        {
            eventBus.Register(this);
            UpdateResources();
        }

        public void Tick()
        {
            UpdateResources();
        }

        [Inject]
        public void InjectDependencies(InventoryService inventoryService, EventBus eventBus)
        {
            this.inventoryService = inventoryService;
            this.eventBus = eventBus;
        }

        [OnEvent]
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
            if (stockCounter.TryGetValue(districtCenter, out var districtStockCounter))
            {
                if (districtStockCounter.TryGetValue(goodId, out var goodStats))
                {
                    if (goodStats[0] == 0)
                        result = 0f;
                    else if (goodStats[1] == 0)
                        result = 1f;
                    else
                        result = Mathf.Clamp01((float)goodStats[0] / goodStats[1]);
                }
                else
                {
                    result = 0f;
                }
            }
            else
            {
                result = 0f;
            }

            return result;
        }

        private void UpdateResources()
        {
            ResetCounter();
            foreach (var inventory in inventoryService.PublicOutputInventories) AddInventoryToCounter(inventory);
        }

        private void ResetCounter()
        {
            /*Console.WriteLine("Resouce Counter:");*/
            foreach (var counts in stockCounter.Values.SelectMany(goods => goods.Values))
            {
                /*Console.WriteLine(new Tuple<int, int>(counts[0], counts[1]));*/
                counts[0] = 0;
                counts[1] = 0;
            }
            /*Console.WriteLine("----------------------------------");*/
        }

        private void AddInventoryToCounter(Inventory inventory)
        {
            // Add district
            var districtCenter = inventory.GetComponentFast<DistrictBuilding>().InstantDistrict;
            if (!districtCenter) return; // is inventory connected to a district?
            if (!stockCounter.ContainsKey(districtCenter))
                stockCounter.Add(districtCenter, new Dictionary<string, int[]>());

            // Count capacities
            var capacityCache = new List<GoodAmount>();
            inventory.GetCapacity(capacityCache);
            foreach (var good in capacityCache.Where(good => inventory.Gives(good.GoodId)))
                AddAmountToCounter(districtCenter, good.GoodId, good.Amount, 1);

            // Count stock
            foreach (var good in inventory.Stock.Where(good => inventory.Gives(good.GoodId)))
                AddAmountToCounter(districtCenter, good.GoodId, good.Amount, 0);
        }

        private void AddAmountToCounter(DistrictCenter districtCenter, string goodId, int amount, int index)
        {
            if (!stockCounter.TryGetValue(districtCenter, out var districtStockCounter)) return;
            if (districtStockCounter.TryGetValue(goodId, out var goodStats))
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