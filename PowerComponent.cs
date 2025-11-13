using System;
using System.Collections.Immutable;
using System.Linq;
using Bindito.Core;
using Timberborn.GameDistricts;
using Timberborn.MechanicalSystem;
using Timberborn.Persistence;
using Timberborn.SingletonSystem;
using Timberborn.TickSystem;
using Timberborn.Workshops;
using Timberborn.WorkSystem;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace EmploymentAutomation;

public class PowerComponent : TickableComponent, IPersistentEntity
{
    private static readonly ComponentKey EmploymentManagerComponentKey =
        new("EmploymentManagerPowerComponent");

    private static readonly PropertyKey<bool> PowerActiveKey = new("PowerActive");
    private static readonly PropertyKey<float> PowerHighKey = new("PowerHigh");
    private static readonly PropertyKey<float> PowerLowKey = new("PowerLow");

    public bool Available { get; private set; }
    public bool PowerActive { get; private set; }
    public float PowerHigh { get; set; } = 0.75f;
    public float PowerLow { get; set; } = 0.25f;

    public Vector2Int EmploymentBounds { get; private set; }

    private Manufactory manufactory;
    private MechanicalNode mechanicalNode;
    private MechanicalNodeSpec mechanicalNodeSpecification;
    private Workplace workplace;

    public void Save(IEntitySaver entitySaver)
    {
        var component = entitySaver.GetComponent(EmploymentManagerComponentKey);
        component.Set(PowerActiveKey, PowerActive);
        component.Set(PowerLowKey, PowerLow);
        component.Set(PowerHighKey, PowerHigh);
    }

    public void Load(IEntityLoader entityLoader)
    {
        try
        {
            var component = entityLoader.GetComponent(EmploymentManagerComponentKey);
            PowerActive = component.Get(PowerActiveKey);
            PowerLow = component.Get(PowerLowKey);
            PowerHigh = component.Get(PowerHighKey);
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
        UpdateComponents();
    }

    private void UpdateComponents()
    {
        try
        {
            workplace = GetComponent<Workplace>();
            manufactory = GetComponent<Manufactory>();
            mechanicalNode = GetComponent<MechanicalNode>();
            mechanicalNodeSpecification = GetComponent<MechanicalNodeSpec>();
            // Don't perform power management when an other mod adds power automation
            Available = !AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes())
                .Any(x => x.Namespace is "IgorZ.SmartPower.Core");
        }
        catch (Exception _)
        {
            Available = false;
        }
    }

    public override void Tick()
    {
        if (!Available || !manufactory.HasCurrentRecipe) return;
        float powerMeter;

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

        // employment trigger bounds
        EmploymentBounds = GetEmploymentBoundsPower(powerMeter);
    }

    private int GetCurrentDesiredWorkers()
    {
        return workplace.DesiredWorkers;
    }

    private Vector2Int GetEmploymentBoundsPower(float powerMeter)
    {
        return new Vector2Int(
            powerMeter < PowerHigh ? 0 : workplace.MaxWorkers, // min
            powerMeter < PowerLow ? 0 : workplace.MaxWorkers); // max
    }
}