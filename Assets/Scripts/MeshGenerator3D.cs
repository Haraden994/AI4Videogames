using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator3D : MonoBehaviour
{
    private struct GridCell
    {
        public bool value { get; private set; }
        public int tileX, tileY, tileZ;
    }
}
