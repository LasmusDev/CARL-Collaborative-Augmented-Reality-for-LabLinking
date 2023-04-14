using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Objects that have this component attached are considered goals for the purposes of distance experiments.
/// See <see cref="GoalSpawner"/> for more details.
/// </summary>
public class GoalComponent : MonoBehaviour
{
    public GoalTag goalTag;
    
    public float DistanceToClosest()
    {
        IEnumerable<GoalTagged> validObjects = FindObjectsOfType<GoalTagged>().Select(x => x.GetComponent<GoalTagged>()).Where(x => x != null && x.isActiveAndEnabled && x.goalTag.Equals(goalTag));
        GoalTagged closest = validObjects.ToList().OrderBy(x => Vector3.Distance(x.transform.position, this.transform.position)).First();
        return Vector3.Distance(closest.transform.position, this.transform.position);
    }
}
