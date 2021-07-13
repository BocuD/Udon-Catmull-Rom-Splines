using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEngine;
using VRC.SDKBase;

public class SplineTester : UdonSharpBehaviour
{
	public CatmullRomSpline spline;
	public float t;
	public float dt = 0.01f;
	
	private void FixedUpdate()
	{
		if (Utilities.IsValid(spline))
		{
			t += dt * Time.fixedDeltaTime;
			if (t >= 1)
				t = 0;
			transform.position = spline.GetWorldSpacePosition(t);
		} 
	}
}