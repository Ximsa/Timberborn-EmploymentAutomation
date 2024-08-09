using System;
using HarmonyLib;
using Timberborn.ModManagerScene;

namespace EmploymentAutomation
{
    public class Plugin : IModStarter
    {
        public void StartMod()
        {
            Console.WriteLine("Hello EmploymentAutomation!");
            Harmony harmony = new Harmony("EmploymentAutomation");
            harmony.PatchAll();
        }
    }
}
