/*=========================================================
	PARTICLE PRO FX volume one 
	PPFXSpawnOnClick.cs
	
	Spawns prefabs at mouseposition when user clicks 
	on object with assigned tag
	
	(c) 2014
=========================================================*/

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PPFXSpawnOnClick : MonoBehaviour {
 
	public GameObject inst;
	public string tagName;
	
	GameObject container;
	
	void Start()
	{
		container = new GameObject();
		container.name = "_Container";
	}
	
	void Update ()
    {
		if((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||  (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed))
    	{
        
         
	        Ray ray;
	       
	        if(Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
	        {
				ray = Camera.main.ScreenPointToRay(Touchscreen.current.primaryTouch.position.ReadValue());
	        }else{
				ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
	        }
	       
	        RaycastHit hit;
	       
	        if(Physics.Raycast(ray,out hit) && GUIUtility.hotControl == 0)
	        {
	            if(hit.collider.tag==tagName)
	            {                  
	                if(inst!=null)
	                {
	                	Vector3 _p = new Vector3(hit.point.x, 0f, hit.point.z);
	                	var _clone = Instantiate(inst, _p , inst.transform.rotation)as GameObject;
	            	
		            	if(container != null)
		            	{
		            		_clone.transform.parent = container.transform;
		            	}
		            	else
		            	{
		            		container = new GameObject();
							container.name = "_Container";
							_clone.transform.parent = container.transform;
		            	}

	            	}          	
	            	
	            }
	        }
	    }
    }
}