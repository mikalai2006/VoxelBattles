using UnityEngine;

public class TestVehicle : MonoBehaviour
{
    LevelManager levelManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        levelManager = GameObject.FindGameObjectWithTag("LevelManager")?.GetComponent<LevelManager>();

        if (levelManager != null)
        {
            PoolNode node = levelManager.AddVehicle(Vector3.zero, true);
            
            levelManager.AddVehicle(new Vector3(200, 0, 10), false);
            levelManager.AddVehicle(new Vector3(-46, 0, -115), false);
            levelManager.AddVehicle(new Vector3(105, 0, 112), false);
            levelManager.AddVehicle(new Vector3(-146, 0, 86), false);
            levelManager.AddVehicle(new Vector3(25, 0, 92), false);
            levelManager.AddVehicle(new Vector3(-53, 0, -29), false);

            //node.EntityLogic.SmartDespawn(Vector3.up, ForceMode.Force);
        }
    }


}
