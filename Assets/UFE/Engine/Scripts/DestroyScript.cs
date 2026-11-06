using UFE3D;
using UnityEngine;

public class DestroyScript : MonoBehaviour
{
    public int destroyTime = 60; // frames

    void Start()
    {
        UFE.DestroyGameObject(gameObject, destroyTime);
    }
}