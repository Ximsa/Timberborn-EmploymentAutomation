using System;
using Timberborn.ModManagerScene;

namespace EmploymentAutomation.Configuration
{
    public class Plugin : IModStarter
    {
        public void StartMod(IModEnvironment modEnvironment)
        {
            Console.WriteLine("Hello EmploymentAutomation!");
        }
    }
}