using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows changing the attached objects material to a highlight-material if notified by other scripts.
/// </summary>
public class InteractionHighlighter : MonoBehaviour
{
    public Material defaultMaterial;
    public Material whileInteracting;

    public void StartHighlight()
    {
        defaultMaterial = this.gameObject.GetComponent<MeshRenderer>().material;
        this.gameObject.GetComponent<MeshRenderer>().material = whileInteracting;
    }

    public void EndHighlight()
    {
        this.gameObject.GetComponent<MeshRenderer>().material = defaultMaterial;
    }
}
