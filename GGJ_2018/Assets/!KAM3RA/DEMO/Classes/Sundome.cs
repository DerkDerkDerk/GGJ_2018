//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Sundome : MonoBehaviour 
{	
	public Color skyColor			= new Color(0, 		0.5f, 	1f, 	1f);
	public Color nightColor			= new Color(0, 		0, 		0, 		1f);
	public Color dayScatter			= new Color(1f, 	1f, 	1f, 	1f);
	public Color sunrise			= new Color(0.5f, 	1f, 	1f, 	1f);
	public Color sunset				= new Color(0.98f, 	0.7f, 	0f, 	1f);
	public float arcLength			= 0f;
	public float size				= 100f;
	public bool simulate			= true;
	public float simulateSpeed		= 3f; 	 
	public float elevationAngle		= 90f;		
	public float polarAngle			= 0;
	public float radius				= 1f; 
	private Color scatter			= new Color(0.98f, 0.7f, 0f, 1f);
	private Vector3 lightPosition 	= Vector3.zero;
	private Vector3 lightNormal   	= Vector3.zero;
	private Vector3 originNormal	= new Vector3(0, 1f, 0);
	private const float PI			= Mathf.PI;
	private const float PI2			= Mathf.PI * 2f;
	private const float PI_2		= Mathf.PI * 0.5f;
	private Material material 		= null;
	private Light sunLight			= null;
	void Awake() 
	{
		material  = GetComponent<Renderer>().sharedMaterials[0];
		sunLight = transform.root.GetComponentInChildren<Light>();
	}
	void LateUpdate() 
	{
		if (simulate) elevationAngle += Time.deltaTime * simulateSpeed;
		transform.position 	= Camera.main.transform.position;
		elevationAngle		= Roll(elevationAngle);
		float theta			= elevationAngle * Mathf.Deg2Rad;
		float phi			= polarAngle * Mathf.Deg2Rad;
		lightPosition		= SphericalToCartesian(phi, theta, radius);
		originNormal		= new Vector3(0, radius, 0).normalized;
		lightNormal			= lightPosition.normalized;		
		arcLength			= Mathf.Acos(Vector3.Dot(lightNormal, originNormal)) / PI;
		Color sunToEnd		= Color.Lerp(sunrise, sunset, Mathf.Abs(elevationAngle / 180f));
		Color skyToEnd		= Color.Lerp(skyColor, nightColor, Mathf.Abs((elevationAngle - 30f) / 150f));
		scatter				= Color.Lerp(dayScatter, sunToEnd, arcLength) * (1f - arcLength);
		float a				= 1.0f - arcLength;		
		size				= Mathf.Max(Mathf.Pow(a * 3f, 4f) * 0.75f, 8f) * 8f;
		material.SetVector("_skyColor", 	skyToEnd);
		material.SetVector("_Scatter", 		scatter);
		material.SetVector("_Light", 		lightPosition);
		material.SetVector("_LightNormal", 	lightNormal);
		material.SetVector("_Origin", 		originNormal);
		material.SetFloat("_ArcLength", 	arcLength);
		material.SetFloat("_Size", 			size);
		sunLight.transform.LookAt(-lightPosition);
		sunLight.color = new Color32((byte)(scatter.r*255f), (byte)(scatter.g*255f), (byte)(scatter.b*255f), (byte)(scatter.a*255f));
		RenderSettings.fogColor = scatter;
		if (Input.GetKeyUp(KeyCode.M)) simulate = !simulate;
	}
	public static Vector3 SphericalToCartesian(float phi, float theta, float radius)
	{
		Vector3 p = new Vector3();
        p.x = Mathf.Cos(phi) * Mathf.Cos(theta) * radius;
        p.z = Mathf.Sin(phi) * Mathf.Cos(theta) * radius;
        p.y = Mathf.Sin(theta) * radius;
		return p;
    }
	public static Material FindMaterial(string name)
	{
     	Renderer[] all = (Renderer[])GameObject.FindObjectsOfType(typeof(Renderer));
        foreach(Renderer r in all)
		{
	 		foreach (Material m in r.sharedMaterials)
			{
				if (m.name.Equals(name)) return m;
			}
		}
		return null;
	}
	public float Roll(float angle)
	{
		if (angle >  180) return angle - 360;
		if (angle < -180) return angle + 360;
		return angle;
	}
}