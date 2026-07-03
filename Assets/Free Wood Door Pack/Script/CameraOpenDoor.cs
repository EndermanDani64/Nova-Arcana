using UnityEngine;

namespace CameraDoorScript
{
    public class CameraOpenDoor : MonoBehaviour
    {
        private float DistanceOpen = 3;
        [SerializeField] private GameObject text;

        void Update()
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position, transform.forward, out hit, DistanceOpen))
            {
                if (hit.transform.GetComponent<DoorScript.Door>())
                {
                    text.SetActive(true);

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        hit.transform.GetComponent<DoorScript.Door>().OpenDoor();
                    }
                    //else text.SetActive(false);
                }
                else
                {
                    text.SetActive(false);
                }
            }
        }
    }
}