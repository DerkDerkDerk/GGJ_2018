//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KAM3RA;

public class Kart : Actor 
{
	// keep track of the vehicles' wheels
	class Wheel
	{
		public enum WheelSide 	  { Left, Right }
		public enum WheelLocation { Front, Back }
		public CapsuleCollider collider = null;
		public GameObject model	      = null;
		public WheelSide side		  = WheelSide.Left;
		public WheelLocation location = WheelLocation.Front;
		public Wheel(GameObject gameObject)
		{
			model    = gameObject;
			side     = model.name.IndexOf("Left")!=-1  ? WheelSide.Left      : WheelSide.Right;
			location = model.name.IndexOf("Front")!=-1 ? WheelLocation.Front : WheelLocation.Back;
		}
		public void InitCollider()
		{
			if (model != null && collider == null)
			{
				GameObject m 			= new GameObject("Collider_" + model.name);
				m.transform.parent 		= model.transform.parent;
				m.transform.position 	= model.transform.position;
				collider 				= (CapsuleCollider)m.AddComponent<CapsuleCollider>();
				collider.radius 		= model.GetComponent<Renderer>().bounds.size.y / 2f; 
			}
		}
	}
	List<Wheel> wheels 					= new List<Wheel>();
	protected GameObject steeringWheel 	= null;
	protected GameObject seat		   	= null;
	protected GameObject seatExit	   	= null;
	protected ParticleSystem exhaust	= null;
	protected float minSpeed          	= 0f;
	protected float tireTurn			= 0f;
	protected Vector3 tireFrontRotation = Vector3.zero;
	protected Vector3 tireBackRotation  = Vector3.zero;
	protected float rpm					= 0f;
	protected Actor rider 				= null;
	protected float nameTick 			= 0f;
	protected string saveNameTag 		= null;
	protected override void Start() 
	{
		base.Start();	
		saveNameTag					= nameTag;
		foreach (Transform t in transform) if (t.name.IndexOf("Wheel")==0) wheels.Add(new Wheel(t.gameObject));
		foreach (Wheel m in wheels) m.InitCollider();
		steeringWheel 				= transform.Find("SteeringWheel").gameObject;
		seat 		  				= transform.Find("Seat").gameObject;
		seatExit 	  				= transform.Find("SeatExit").gameObject;
		exhaust 					= transform.Find("Exhaust").GetComponent<ParticleSystem>();
		exhaust.gameObject.SetActive(false);
		collider.enabled 		 	= false;
		minSpeed 				 	= 0f;
		rigidbody.freezeRotation 	= false;
		rigidbody.angularDrag	 	= 5f;
		rigidbody.centerOfMass 	 	= new Vector3(0f, -1f, 0.1f);
		rigidbody.isKinematic		= true;
	}
	
	public override float Mass
	{
		 get { return User.CalculateMass(collider.bounds.extents.magnitude * 200f); }
	}
	public override void UserUpdate(User user)
	{
		if (type == Actor.Type.Ground)
		{
			if (user.velocity.z < 0f) 
			{
				user.velocity.z = 0f;
			}
			user.velocity.x = 0f;
			if (user.velocity.y > 0f)
			{
				user.velocity.y = 0f;
				speed *= 0.98f;
			}
		}
		base.UserUpdate(user);
	}
	
	//////////////////////////////////////////////////////////////
	// Attach to Camera
	//////////////////////////////////////////////////////////////
	public override void OnPlayerAttached(Actor actor, Actor last)
	{
		base.OnPlayerAttached(actor, last);
		if (actor == this) 
		{
			rider 		= last;
			nameTick 	= 4f;
			showNameTag = true;
			SetNameTag("Press X to Exit, R if Upside-Down");
			if (rider != null) 
			{
				rider.rigidbody.detectCollisions = false;
				rider.State = "Idle";
			}
			if (GetComponent<AudioSource>() != null) GetComponent<AudioSource>().Play();
			exhaust.gameObject.SetActive(true);
			rigidbody.isKinematic = false;
		}
		else if (last == this)
		{
			if (rider != null) 
			{
				SetNameTag(saveNameTag);
				rider.transform.position = seatExit.transform.position;
				rider.rigidbody.detectCollisions = true;
				rider 		= null;
				showNameTag = true;
				if (GetComponent<AudioSource>() != null) GetComponent<AudioSource>().Stop();
				rigidbody.isKinematic = true;
			}
		}
	}
	
	//////////////////////////////////////////////////////////////
	// Audio 
	//////////////////////////////////////////////////////////////
	protected override void UpdateSound()
	{
		if (GetComponent<AudioSource>() != null) GetComponent<AudioSource>().pitch  = Mathf.Min(0.5f + Speed * 0.05f, 2f);
	}	
	protected override void Update()
	{
		base.Update();
		if (player)
		{
			// hide the name tag after a few seconds
			if (nameTick > 0)
			{
				nameTick -= Time.deltaTime; 
				if (nameTick <= 0) showNameTag = false;
			}
			// must not be moving
			if (!User.Instance.Moving)
			{
				// reset on the R key if we're flipped
				if (Input.GetKey(KeyCode.R) && Mathf.Abs(transform.eulerAngles.z) >= 90) 
				{
					Reset();
				}
				// exit the vehicle
				if (Input.GetKey(KeyCode.X))
				{
					if (rider != null)
					{
						User.Instance.Player = rider;
						return;
					}
				}
			}	
			// update rider's transform
			if (rider != null)
			{
				rider.transform.position = seat.transform.position; 
				rider.transform.rotation = transform.rotation;
				// if we're invisible, the ride should be too
				rider.SetEnabled(this.renderer.enabled);
			}
			// turn the steering wheel
			steeringWheel.gameObject.transform.localRotation = Quaternion.Euler(0, 0, -tireTurn);
			// factor to indicate turning
			tireTurn = Mathf.Clamp(User.Instance.angularVelocity.y * 35f, -35f, 35f);
			// change direction if we're in reverse
			if (States.MovingBack) tireTurn = -tireTurn;
			// calculate local velocity and turn the tire models 
			Vector3 localVelocity 	= transform.InverseTransformDirection(rigidbody.velocity);
			rpm 					+= localVelocity.z > 0 ? Speed : localVelocity.z < 0 ? -Speed : 0f;
			tireBackRotation.x  	= tireFrontRotation.x = rpm;
			// front wheels get side-to-side turning as well
			tireFrontRotation.y 	= tireTurn;
			// set the wheels
			foreach (Wheel m in wheels) 
			{
				m.model.transform.localEulerAngles = m.location == Wheel.WheelLocation.Front ? tireFrontRotation : tireBackRotation;
			}
		}
	}
	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		// add some rock n' roll
		if (player && Colliding) 
		{
			rigidbody.AddTorque(new Vector3(0, 0, Random.Range(-0.25f, 0.25f)), ForceMode.VelocityChange);
		}
	}
	public override void Reset()
	{
		base.Reset();
		exhaust.gameObject.SetActive(false);
	}
}
