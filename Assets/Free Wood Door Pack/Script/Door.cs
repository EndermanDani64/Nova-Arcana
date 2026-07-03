using FMODUnity;
using UnityEngine;
namespace DoorScript
{
	public class Door : MonoBehaviour 
	{
		private bool _isOpen;
		private float _smoothnessFactor = 2.5f;
		float _doorOpenAngle = -90.0f;
		float _doorCloseAngle = 0.0f;

		[SerializeField] private StudioEventEmitter _emiterOpen;
		[SerializeField] private StudioEventEmitter _emiterClose;

	
		// Update is called once per frame
		void Update () 
		{
			if (Mathf.Round(transform.localRotation.y * 100) != -71 &&
                Mathf.Round(transform.localRotation.y * 100) != 0)
			{
                gameObject.GetComponent<BoxCollider>().enabled = false;
            }
			else gameObject.GetComponent<BoxCollider>().enabled = true;

			if (_isOpen)
			{
				var target = Quaternion.Euler(0, _doorOpenAngle, 0);
				transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * 1.5f * _smoothnessFactor);

			}
			else
			{
				var target1 = Quaternion.Euler(0, _doorCloseAngle, 0);
				transform.localRotation = Quaternion.Slerp(transform.localRotation, target1, Time.deltaTime * 1.5f * _smoothnessFactor);
			}
		}

		public void OpenDoor()
		{
			if (_isOpen)
			{
                _emiterClose.Play();
                _isOpen = false;
				
            }
			else
			{
                _emiterOpen.Play();
                _isOpen = true;
            }
        }
	}
}