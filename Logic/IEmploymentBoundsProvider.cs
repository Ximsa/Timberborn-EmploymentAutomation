using UnityEngine;

namespace EmploymentAutomation.Logic;

public interface IEmploymentBoundsProvider
{
    public Vector2Int EmploymentBounds { get; } 
}