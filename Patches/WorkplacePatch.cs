using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using Timberborn.WorkSystem;

namespace EmploymentAutomation.Patches;

[HarmonyPatch(typeof(Workplace))]
[UsedImplicitly]
internal class WorkplacePatch
{
    [HarmonyFinalizer]
    [HarmonyPatch(nameof(Workplace.DecreaseDesiredWorkers))]
    [HarmonyTranspiler]
    [UsedImplicitly]
    private static IEnumerable<CodeInstruction> DecreaseDesiredWorkersTranspiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var enumeratedInstructions = instructions as CodeInstruction[] ?? instructions.ToArray();
        foreach (var instruction in enumeratedInstructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_1)
            {
                instruction.opcode = OpCodes.Ldc_I4_0;
                break;
            }
        }

        return enumeratedInstructions;
    }
}