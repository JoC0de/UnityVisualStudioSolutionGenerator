using RootTestProject.Module1;
using RootTestProject.Module2;
using UnityEngine;

public class RootScript : MonoBehaviour
{
    private void OnEnable()
    {
        var cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube1.name = Module1Script.GetName();
        cube1.transform.parent = transform;
        cube1.transform.position = Vector3.left;
        var cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube2.name = Module2Script.GetName();
        cube2.transform.parent = transform;
        cube2.transform.position = Vector3.right;
    }
}
