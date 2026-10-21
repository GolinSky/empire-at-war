using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PPFXClickMove : MonoBehaviour {
	
	public float speed = 5f;
	public string tagName = "plane";
	
	Vector3 pos = new Vector3(0,0,0);
	
	void Start()
	{
		pos = transform.position;
	}
	
	void Update () {
		//check if user has clicked
		if((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame))
		{    
			
			Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
			RaycastHit hit;
			if(Physics.Raycast(ray,out hit))
			{
	        	//check if user has clicked on tagged object
				if(hit.collider.tag==tagName)
				{             
					pos = hit.point;
				}
			}
		}
		
		float step = speed * Time.deltaTime;
		transform.position = Vector3.MoveTowards(transform.position, pos, step);
	}
}
