//////////////////////////////////////////////////////////////
//  KAM3RA Third-Person Camera System
// 	Copyright © 2013 Regress Software
//////////////////////////////////////////////////////////////

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace KAM3RA 
{ 
	public class Actor : MonoBehaviour 
	{
		//////////////////////////////////////////////////////////////
		// Public Variables
		//////////////////////////////////////////////////////////////
		// actor type -- we do things slightly different depending on whether we're ground, fly or hover
		public Type type							= Type.Ground;
		
		// true if attached to User
		public bool player							= false;
		
		// rigidbody we will find or create
		new public Rigidbody rigidbody				= null;
		
		// CapsuleCollider we will find or create
		new public CapsuleCollider collider			= null;
		
		// main actor body, retrieved in InitRenderers()
		new public Renderer renderer				= null;
		
		// main actor animation, retrieved in InitAnimation() -- this the first Animation component we can find
		new public Animation animation				= null;

		// hide or show name tag
		public bool showNameTag						= true;
		
		// name tag name
		public string nameTag						= "Actor";
		
		// name tag default color
		public Color nameTagColor					= Color.green;
		
		// maximum angle we collide at and still keep moving 
		// NOTE this is unsigned -- use with altitudeScale (below) 
		// to know whether you're on an upward or downward slope
		public float maxSlope						= 80f;
		
		// sets rigidbody.drag when not 1) Idling (velocity == 0, drag = idleDrag) or 2) In-air (drag = 0)
		public float drag							= 10f;
		
		// how high the actor can jump 
		public float jumpHeight 					= 2f;
		
		// where the camera is looking through the actor, 0-1 is from feet (presumed to be the actor's local position) to top of the head
		public float eyeHeightScale					= 0.9f;
	
		// maximum speed of the actor we're controlling
		public float maxSpeed						= 4f;
		
		// accleration - ramp up to speed, expressed as a percentage (0-1) of maxSpeed
		public float acceleration					= 1f;
		
		// momentum -- dead-stop is zero 
		public float momentum 						= 0f;
	
		// current collision state -- None, Grounded or Blocked
		public CollisionState collisionState		= CollisionState.None;
		
		// current target, if any
		public Actor target							= null;	
		
		// animation mapper and current state output for actor info and animation
		public StateMap state						= new StateMap();

		// 3D text mesh above the actor's name
		public TextMesh nameTagMesh 				= null;
		
		// 3D text mesh offset
		public Vector3 nameTagOffset				= Vector3.zero;
		
		// collide radius
		public float radius			 				= 0.3f;
		
		//////////////////////////////////////////////////////////////
		// Protected Variables
		//////////////////////////////////////////////////////////////		
		// list of all Renderers that are children of this gameObject -- we need all Renderers to toggle visiblity on/off
		protected List<Renderer> renderers 			= new List<Renderer>();
		
		// highest collision point for the current collision
		protected ContactPoint maxCollisionPoint	= new ContactPoint();
	
		// lowest collision point for the current collision
		protected ContactPoint minCollisionPoint	= new ContactPoint();

		// if we're currently colliding with something > maxSlope AND lower than us
		protected bool stuck						= false;
		
		// per-frame velocity 
		protected Vector3 velocity  				= Vector3.zero;	
	
		// current non-scaled input speed
		protected float speed	  					= 0f;	
	
		// whether or not to look at the target when there is one
		protected bool watchTarget 					= false;

		// tracks velocity.y after after a jump 
		protected float fallVelocity				= 0f;	
		
		//if fallVelocity is below this veloocity, automatically go into a "fall" state
		protected float fallVelocityCutoff			= -3f;	
	
		// sets rigidbody.drag when idle -- trumps drag to prevent "slippage" of the actor on slopes when idling
		protected float idleDrag					= 100f;
		
		// current distance from last collision while "in the air"
		protected float relativeAltitude			= 0f;
		
		// tracks how long we're in a non-collision state, applies when setting user input only
		protected float airTime						= 0f;
		
		// time at which we're eligible to fly 
		protected float canFlyAtTime				= 0.5f;
		
		// current y position change
		protected AltitudeState altitudeState		= AltitudeState.Level;
		
		//////////////////////////////////////////////////////////////
		// Private Variables
		//////////////////////////////////////////////////////////////		
		// name tag start color -- save
		private Color nameTagStartColor				= Color.green;

		// name tag material -- for editor only
		private Material nameTagMaterial			= null;
		
		//////////////////////////////////////////////////////////////
		// Init 
		//////////////////////////////////////////////////////////////
		// not currently implemented by Actor
		protected virtual void Awake()
		{
			// sub
		}
		// init functions as well as a check to see if we're staring out as the player
		protected virtual void Start()
		{
			// store name tag color
			nameTagStartColor = nameTagColor;
			
			// required
			if (!InitRenderers()) 
			{
				Debug.Log(name + "(Actor) could not initialize Renderers.");
				return;
			}
			// required
			if (!InitPhysics()) 
			{
				Debug.Log(name + "(Actor) could not initialize Physics.");
				return;
			}
			// optional
			if (!InitNameTag())	  
			{
				Debug.Log(name + "(Actor) does not have TextMesh, OK...");
			}
			// optional
			if (!InitAnimation()) 
			{
				Debug.Log(name + "(Actor) does not have Animation, OK...");
			}
			
			// register for attachment notification
			User.Instance.onPlayerAttached += OnPlayerAttached;
			
			// if we're starting out as player -- only one actor should start as player!
			if (player) User.Instance.Player = this;
		}
		// first, fill the renderers list with all renderers 
		// for hiding them when the camera is too close to the actor
		// next, find the main actor's Renderer -- we're using a simple
		// heuristic here which is 	1) model has bones (SkinnedMeshRenderer), and
		//                         	2) model has the greatest number of bones, or
		//							3) if model has no bones, the largest Renderer present
		protected virtual bool InitRenderers()
		{
			renderers = new List<Renderer>(GetComponentsInChildren<Renderer>(true)); 
			if (renderers.Count > 0)
			{
				int maxBones = 0; // we're looking for the SkinnedMeshRenderer with the most bones
				foreach (Renderer m in renderers) 
				{
					if (m is SkinnedMeshRenderer) 
					{
						int b = ((SkinnedMeshRenderer)m).bones.Length;
						if (b > maxBones) { maxBones = b; renderer = m; }
					}
				}
				// no SkinnedMeshRenderers, look for the largest renderer
				if (renderer == null) 
				{
					float h = 0;
					foreach (Renderer m in renderers) 
					{
						if (m.bounds.size.y > h)
						{
							h = m.bounds.size.y; 
							renderer = m;
						}
					}
				}
				// this should never happen
				if (renderer == null)
				{
					renderer = renderers[0];
				}
			}
			return (renderer != null);
		}	
		// find the first Animation we come to -- null is legal
		protected virtual bool InitAnimation()
		{
			animation = GetComponent<Animation>();
			if (animation == null)
			{
				foreach (Transform t in transform)
				{
					animation = t.GetComponent<Animation>();
					if (animation != null) break;
				}
			}
			return (animation != null);
		}
		// setup Physics - Rigidbody and CapsuleCollider objects
		protected virtual bool InitPhysics()
		{
			// it may be already attached to this gameObject
			rigidbody = GetComponent<Rigidbody>(); 
			bool rigidbodyWasNull = rigidbody == null;

			// if not, add it					
			if (rigidbody == null) rigidbody = (Rigidbody)gameObject.AddComponent<Rigidbody>();
			
			// it may be already attached to this gameObject
			collider = GetComponentInChildren<CapsuleCollider>(); 

			// if not, add it					
			if (collider == null) collider = (CapsuleCollider)gameObject.AddComponent<CapsuleCollider>();
			
			// collider properties
			if (collider != null)
			{
				Vector3 scale 		 = transform.localScale; 										// we don't want scale in this calculation
				transform.localScale = Vector3.one;													// store scale
				collider.radius 	 = radius;														// this generally needs to be relatively tight
				collider.height 	 = renderer.bounds.size.y; 										// fit the capsule's height to the actor's actual "starting" height
				collider.center		 = User.SetVectorY(collider.center, collider.height * 0.5f); 	// center the capsule's at half height
				transform.localScale = scale;														// restore scale
			}
			
			// rigidbody properties
			if (rigidbody != null)
			{
			    rigidbody.freezeRotation = true;													// we don't want Physics to modify the actor's rotation
			    rigidbody.useGravity 	 = true;													// we do want to use gravity
				rigidbody.drag 			 = 0f;														// we start out with the specified drag from groundDrag
				rigidbody.angularDrag	 = 0f;														// no angular drag
				rigidbody.isKinematic	 = false;													// not kinematic -- we're using forces to affect this rigidbody
				rigidbody.mass			 = rigidbodyWasNull ? Mass : rigidbody.mass;				// set a reasonable mass
			}
	
			// required: valid collider and rigidbody
			return (collider != null && rigidbody != null);
		}
		// name tag that floats above this object
		protected virtual bool InitNameTag()
		{
			nameTagMesh = GetComponentInChildren<TextMesh>();	
			if (nameTagMesh != null)
			{
				nameTagMesh.offsetZ				= 0f;
				nameTagMesh.characterSize 		= 0.05f;
				nameTagMesh.lineSpacing   		= 0f;
				nameTagMesh.anchor				= TextAnchor.MiddleCenter;
				nameTagMesh.alignment			= TextAlignment.Center;
				nameTagMesh.tabSize				= 0f;
				nameTagMesh.fontSize			= 32;
				nameTagMesh.fontStyle			= FontStyle.Normal;
				nameTagMesh.richText			= false;
				SetNameTagColor(nameTagColor);
				SetNameTag(nameTag);
				nameTagMesh.GetComponent<Renderer>().enabled = true; // we need to set this explicity here since it might get hidden by the inspector
			}
			return (nameTagMesh != null);
		}
		
		//////////////////////////////////////////////////////////////
		// KAM3RA
		//////////////////////////////////////////////////////////////
		// callback from User when an actor is "attached" 
		// actor -- actor attached
		// last  -- last actor attached
		public virtual void OnPlayerAttached(Actor actor, Actor last)
		{
			player = actor == this;
			if (player || last == this) Reset();
		}
		// called by User for the current Actor attached to it
		public virtual void UserUpdate(User user)
		{
			// these values are 0 or 1 -- not variable
			Vector3 userVelocity = user.velocity;
			
			// range varies based on User sensitivity and damping
			Vector3 userAngular  = user.angularVelocity;
			
			// rotate this transform to keep the actor facing forward
			transform.Rotate(userAngular, Space.World);	
			
			// no reverse and no side-to-side movement flying
			if (type == Type.Fly) 
			{
				if (userVelocity.z < 0f) userVelocity.z = 0f;
				userVelocity.x = 0f;
			}
			
			// if not in air
			if (Colliding)
			{
				// always use gravity when not in air
				rigidbody.useGravity = true;
				// reset air time tracker
				airTime 			 = 0f;
				// if flying, no jumping at all 
				if (type == Type.Fly)
				{
					if (speed == maxSpeed)
					{
						State = "Fly";
						userVelocity.y = 1f;
					}
				}
				else 
				{
					// we're jumping, set jump and exit 
					if (userVelocity.y != 0f)
					{
						State  		= "Jump";
						velocity 	= transform.TransformDirection(userVelocity) * ScaledSpeed * 0.5f;
						velocity.y 	= JumpVelocity;
						return;
					}
				}
				// otherwise check to see if we're moving normally or are "walking"
				bool walk = Mathf.Abs(userVelocity.z) == user.walkScale;
				if 		(userVelocity.z > 0f) State = walk ? "WalkForward" : "RunForward";
				else if (userVelocity.z < 0f) State = walk ? "WalkBack"    : "RunBack";
				else if (userVelocity.x > 0f) State = "StrafeRight";
				else if (userVelocity.x < 0f) State = "StafeLeft";
				else 
				{
					// not moving, so might be turning
					if (Mathf.Abs(userAngular.y) <= 1f) State = "Idle";
					else 	  if (userAngular.y   > 1f) State = "TurnRight";
					else 	  if (userAngular.y   < 1f) State = "TurnLeft";
				}
				// we're damping velocity in Update() so we only set non-zero velocity here
				if (userVelocity != Vector3.zero) velocity = transform.TransformDirection(userVelocity) * ScaledSpeed;
			}
			else if (type != Type.Ground)
			{
				// no side-to-side movement whether flying or hovering, if not colliding
				userVelocity.x = 0f;
				// if we're not already flying
				if (State != "Fly")
				{
					// we have a timer here so we can still jump
					airTime = Mathf.Min(airTime + Time.deltaTime, canFlyAtTime);
					// if user is holding down the "jump" key and we've passed a little time
					if (userVelocity.y != 0 && airTime == canFlyAtTime) State = "Fly";
				}
				else
				{
					// no gravity if hovering
					rigidbody.useGravity = (type == Type.Fly);					
					// if moving forward, so set to the current direction, otherwise, zero it out
					velocity  		= userVelocity.z > 0 ? user.Direction * MaxScaledSpeed : Vector3.zero;
					// but if we're still on the "jump" button, apply
					if (type == Type.Hover) velocity.y += userVelocity.y * MaxScaledSpeed * 2f;
					else  					velocity.y += userVelocity.y * MaxScaledSpeed * 0.5f;
				}
			}
		}
		
		//////////////////////////////////////////////////////////////
		// Updates 
		//////////////////////////////////////////////////////////////
		protected virtual void Update() 
		{	
			// if not "in the air", adjust speed per accleration 
			if (Colliding)
			{
				float accel = acceleration * maxSpeed;
				speed       = accel >= maxSpeed ? maxSpeed : (Mathf.Clamp(speed + Time.deltaTime * (States.Moving ? accel : -accel * 4f), 0f, maxSpeed));
			}
			// back off on velocity every frame -- only if gravity
			if (!States.MovingOrJumping) 
			{
				if (rigidbody.useGravity)
				{
					velocity *= momentum;
				}
			}
			// if we have a target, stay put, and watch it if requested
			if (!player && target != null)
			{
				velocity = Vector3.zero;
				State    	 = "Idle";
				if (watchTarget) User.LookAt2D(transform, target.transform.position);
			}
			// note that name tags are 3D text whose parent is this transform, and 
			// we're NOT offsetting scale... so the name tag gets larger for larger scales
			if (nameTagMesh != null)
			{
				nameTagMesh.gameObject.SetActive(showNameTag);
				if (showNameTag)
				{
					nameTagMesh.transform.rotation = User.Instance.GetComponent<Camera>().transform.rotation;
					nameTagMesh.transform.position = User.SetVectorY(transform.position, 
						transform.position.y + collider.height * transform.localScale.y + nameTagMesh.GetComponent<Renderer>().bounds.size.y * 2f) + nameTagOffset;

				}
			}
		}
		protected virtual void LateUpdate()
		{
			// set the animation state here
			if (animation != null) animation.CrossFade(States.Name);
			
			// update sound -- not implemented in Actor but subs would implement
			UpdateSound();
		}
		
		//////////////////////////////////////////////////////////////
		// Sound 
		//////////////////////////////////////////////////////////////
		protected virtual void UpdateSound()
		{
			// sub
		}
		
		//////////////////////////////////////////////////////////////
		// Physics 
		//////////////////////////////////////////////////////////////
		protected virtual void FixedUpdate() 
		{
			// if idle, apply more drag to keep the actor from slipping
			// if Blocked or not colliding, cancel drag, we need to fall at maximum rate
			rigidbody.drag = collisionState == CollisionState.Grounded ? drag : 0f;
			
			// if we're currently on the ground
			if (Colliding) 
			{
				// if we're either Blocked or not colliding -- if Blocked we're stuck at maxSlope
				if (collisionState == CollisionState.Blocked)
				{
					velocity   += maxCollisionPoint.normal;
					velocity.y = -collider.height;
				}

				if (velocity == Vector3.zero) rigidbody.drag = idleDrag;
				
				// add the velocity we set in Velocity, subtract the current rigidbody velocity since we're adding an incremental change
				rigidbody.AddForce(velocity - rigidbody.velocity, ForceMode.VelocityChange);

				// if jumping, set the current velocity's y-component directly
				if (collisionState == CollisionState.Grounded)
				{
					if (State != "Jump" && State != "Fly" && TooFast) 
					{
						if (momentum == 0)
						{
							rigidbody.drag     = idleDrag;
							rigidbody.velocity = -rigidbody.velocity.normalized * ScaledSpeed;
						}
					}
				}
				
				// reset fall velocity and relative altitude trackers
				fallVelocity 	 = 0f;
				relativeAltitude = 0f;
		    }
			else 
			{
				// track relative altitude
				relativeAltitude = transform.position.y;
				if (State == "Fly") 
				{
					// is using gravity, only set velocity if it's not equal to zero
					if (rigidbody.useGravity)
					{
						if (velocity != Vector3.zero) rigidbody.velocity = velocity;
					}
					// otherwise set the velocity -- if it's zero, the actor will just sit in the air
					else 
					{
						rigidbody.velocity = velocity;
					}
				}
				else 
				{
					// if we're not in a fall state
					if (State != "Fall")
					{
						// we're falling at any velocity no matter how small 
						if (rigidbody.velocity.y < 0f) 
						{
							// add them until we get to a reasonable cutoff
							fallVelocity += rigidbody.velocity.y;
							// now we're really falling!
							if (fallVelocity < fallVelocityCutoff)
							{
								State = "Fall";
							}
						}
					}
				}
				
				// if we're "stuck" and moving, probably trying to jump over something, give a little boost
				if (stuck && States.Moving) 
				{
					rigidbody.AddForce(velocity * 0.25f, ForceMode.VelocityChange);
					stuck = false;
				}
			}	
			// if moving, set an altitude state -- Level, Up or Down
			if (States.Moving)
			{
				altitudeState = (Math.Abs(rigidbody.velocity.y) < 1f ? AltitudeState.Level : (rigidbody.velocity.y > 0 ? AltitudeState.Up : AltitudeState.Down));
			}			
		}
		// Unity continual collision callback
		protected virtual void OnCollisionStay(Collision collision) 
		{
			// if this method has been called, we're in some kind of collision state
			collisionState = CollisionState.Grounded;
			float max      = WaistPosition.y;
			float min      = WaistPosition.y;
			
			// check collision points
			foreach (ContactPoint m in collision.contacts) 
			{
				// how steep is the point with which we collided? 
				float slope = Mathf.Acos(Mathf.Clamp(m.normal.y, -1f, 1f)) * Mathf.Rad2Deg;
				
				// if it's fairly steep, flag stuck and we may be trying to jump
				stuck = (slope > maxSlope / 2f);
				
				// max collision extrema
				if (m.point.y >= max)
				{
					maxCollisionPoint = m;
					max 			  = m.point.y;
					// if greater than our max grade AND the point's height is higher than the actor's waist
					if (slope > maxSlope) collisionState = CollisionState.Blocked;
				}
				// min collision extrema
				if (m.point.y <= min)
				{
					minCollisionPoint = m;
					min 			  = m.point.y;
				}
			}
		}		
		// we stoped colliding -- usually this means we're in the air
		protected virtual void OnCollisionExit()
		{
			collisionState = CollisionState.None;
		}
		// returns true if colliding – this is usually all the time unless jumping or falling
		public virtual bool Colliding
		{
			get { return collisionState != CollisionState.None;  }
		}	
		// reset rigidbody, rotation and defaul to Idle
		public virtual void Reset()
		{
			State = "Idle";
			speed = 0f;
			transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
			if (rigidbody != null)
			{
				if (!rigidbody.isKinematic) rigidbody.velocity = rigidbody.angularVelocity = Vector3.zero;
				velocity 				= Vector3.zero;
				rigidbody.isKinematic 	= true;
				rigidbody.isKinematic 	= false;
			}
			SetEnabled(true);
			ResetNameTagColor();
		}
	
		//////////////////////////////////////////////////////////////
		// Other
		//////////////////////////////////////////////////////////////
		public virtual void SetEnabled(bool enabled) 
		{ 
			foreach (Renderer m in renderers) m.enabled = enabled;
		}
		public virtual void SetTarget(Actor target)
		{
			SetTarget(target, false);
		}
		public virtual void SetTarget(Actor target, bool watch)
		{
			this.target = target;
			watchTarget = watch;
		}
		public virtual float Distance(Actor other)
		{
			return Vector3.Distance(transform.position, other.transform.position);
		}
		public virtual void ResetNameTagColor()
		{
			SetNameTagColor(nameTagStartColor);
		}
		public TextMesh GetNameTagMesh()
		{
			return nameTagMesh;
		}
		public virtual void ShowNameTag(bool show)
		{
			showNameTag = show;
			// for custom inspector only
			if (!Application.isPlaying)
			{
				TextMesh m = GetComponentInChildren<TextMesh>();
				if (m != null) m.GetComponent<Renderer>().enabled = showNameTag;
			}
		}
		public virtual void SetNameTagColor(Color color)
		{
			nameTagColor = color;
			if (nameTagMesh != null) 
			{
				nameTagMesh.GetComponent<Renderer>().material.color = nameTagColor;
			}
			// for custom inspector only
			if (!Application.isPlaying)
			{
				TextMesh m = GetComponentInChildren<TextMesh>();
				if (m != null) 
				{
					if (nameTagMaterial == null) nameTagMaterial = new Material(m.GetComponent<Renderer>().sharedMaterial);
					nameTagMaterial.color = nameTagColor;
					m.GetComponent<Renderer>().material   = nameTagMaterial;
				}
			}
		}
		public virtual void SetNameTag(string name)
		{
			nameTag = name;
			if (nameTagMesh != null) nameTagMesh.text = nameTag;
			// for custom inspector only
			if (!Application.isPlaying)
			{
				TextMesh m = GetComponentInChildren<TextMesh>();
				if (m != null) m.text = nameTag;
			}
		}
		public virtual void AddUniformScale(float scale)
		{
			transform.localScale += new Vector3(scale, scale, scale);
		}
		public virtual void PrintPositions()
		{
			if (player) User.PrintPositions(transform);
		}
		
		//////////////////////////////////////////////////////////////
		// State Properties 
		//////////////////////////////////////////////////////////////
		public StateMap States 						{ get { return state;  } }
		public string State 						{ get { return state.state;  } set { state.state = value; } }
		
		//////////////////////////////////////////////////////////////
		// Other Properties 
		//////////////////////////////////////////////////////////////
		// current bounding radius based on renderer -- keep in mind this could change depending on animation
		// also NOTE that ParticleSystems and TextMesh are rightly not included in the computation
		public virtual float RendererBoundingRadius 
		{ 
			get 
			{ 
				Bounds bounds = new Bounds(transform.position, Vector3.zero);
				foreach (Renderer m in renderers) 
				{
					if (m.gameObject.GetComponent<ParticleSystem>() != null) continue;
					if (m.gameObject.GetComponent<TextMesh>()       != null) continue;
					bounds.Encapsulate(m.bounds);
				}
				return bounds.extents.magnitude; 
			} 
		}
		// bounding radius based on colliders
		public virtual float BoundingRadius 
		{ 
			get 
			{ 
				List<Collider>colliders = new List<Collider>(GetComponentsInChildren<Collider>(true));
				Bounds bounds = new Bounds(transform.position, Vector3.zero);
				foreach (Collider m in colliders) bounds.Encapsulate(m.bounds);
				return bounds.extents.magnitude; 
			} 
		}
		// reasonable mass
		public virtual float Mass 					{ get { return User.CalculateMass(collider.bounds.extents.magnitude); 			} }
		// moving a bit faster than desired speed
		public virtual bool TooFast 				{ get { return Speed > ScaledSpeed; 											} }
		// ... or a bit slower
		public virtual bool TooSlow					{ get { return Speed < ScaledSpeed;												} }
		// actual current speed, *not* desired speed
		public virtual float Speed					{ get { return rigidbody.velocity.magnitude; 									} }
		// current horizontal speed, *not* desired speed
		public virtual float HorizontalSpeed		{ get { return User.SetVectorY(rigidbody.velocity, 0f).magnitude;				} }
		// current speed adjusted by scale height of the object
		public virtual float ScaledSpeed 			{ get { return speed * Height; 													} }
		// max speed adjusted by scale height of the object
		public virtual float MaxScaledSpeed 		{ get { return maxSpeed * Height; 												} }
		// current position + adjustment for where the "eyes" are
		public virtual Vector3 EyePosition 			{ get { return User.AddVectorY(transform.position, Height * eyeHeightScale); 	} }
		// same as above but the middle of the object
		public virtual Vector3 WaistPosition 		{ get { return User.AddVectorY(transform.position, Height * 0.5f); 				} }
		// same as above but the bottom of the object
		public virtual Vector3 FootPosition 		{ get { return User.AddVectorY(transform.position, Height * 0.1f); 				} }
		// simple collider radius
		public virtual float Radius					{ get { return radius; 															} }
		// we could use renderer.bounds.size.y * eyeHeightScale here, but that changes per frame a bit
		public virtual float Height 				{ get { return collider.height * transform.localScale.y; 						} }
		// jump adjusted for scale and gravity
		public virtual float JumpVelocity			{ get { return Mathf.Sqrt(jumpHeight * Height * -Physics.gravity.y * 2f);		} }
		
		//////////////////////////////////////////////////////////////
		// Enums
		//////////////////////////////////////////////////////////////
		// major difference between Fly and Hover is gravity (when in the air -- hoverers have gravity when on ground and jumping)
		public enum Type 			{ Ground, Fly, Hover }
		public enum CollisionState 	{ None, Grounded, Blocked }
		public enum AltitudeState 	{ Level, Up, Down }		
		
		//////////////////////////////////////////////////////////////
		// Other Classes
		//////////////////////////////////////////////////////////////
		[Serializable]
		public class StateMap 
		{
			public string state = "Idle";
			public List<StateName> map = new List<StateName>();
			static string[] MOVING = new string[] { "RunForward", "RunBack", "StrafeRight", "StafeLeft", "WalkForward", "WalkBack" };
			public StateMap()
			{
				// these are the defaults germane to the test model, however they 
				// obviously be whatever you want them to be...
				// you can change the defaults here to give yourself a leg up
				// otherwise change, add or delete them via the inspector
				// NOTE that the Actor class use these first 12 states so change/delete with caution
				Add("Idle", 		"Idle");  		// no input
				Add("WalkForward", 	"Walk"); 		// walking forward at half ScaledSpeed
				Add("WalkBack", 	"Walk"); 		// walking back at half ScaleSpeed
				Add("RunForward", 	"Run"); 		// moving forward at ScaledSpeed
				Add("RunBack", 		"Run"); 		// moving back and ScaledSpeed
				Add("TurnLeft", 	"Shuffle");		// *not* moving and turning left with key/mouse
				Add("TurnRight", 	"Shuffle"); 	// *not* moving and turnign right with key/mouse
				Add("StafeLeft", 	"Walk"); 		// side-to-side left
				Add("StrafeRight", 	"Walk"); 		// side-to-side right
				Add("Jump", 		"Jump");		// start a jump
				Add("Fall", 		"Fall");		// after a certain velocity.y threshold, fall
				Add("Fly", 			"Fly");			// flying in-air
			}
			public string Name 								{ get { foreach (StateName m in map) if (m.state == state) return m.name; return "Idle"; } }
			public bool Moving 								{ get { return IsState(MOVING); 									} }
			public bool MovingForward 						{ get { return state == "WalkForward" || state == "RunForward"; 	} }
			public bool MovingBack 							{ get { return state == "WalkBack"    || state == "RunBack"; 		} }
			public bool Walking 							{ get { return state == "WalkForward" || state == "WalkBack"; 		} }
			public bool Jumping 							{ get { return state == "Jump"; 									} }
			public bool MovingOrJumping 					{ get { return Jumping || Moving; 									} }
			public bool IsState(string[] states) 			{ foreach (string m in states) if (state == m) return true; return false; }
			public StateName Add(string state, string name) { StateName m = new StateName(state, name); map.Add(m); return m; }
			public void Remove(StateName m) 				{ map.Remove(m); }
		}
		[Serializable]
		public class StateName
		{
			public string state = "Idle", name = "Idle";
			public StateName(string state, string name) { this.state = state; this.name = name; }
		}
	}
}