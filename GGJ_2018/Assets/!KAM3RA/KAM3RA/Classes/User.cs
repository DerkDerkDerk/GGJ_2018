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
	public class User : MonoBehaviour 
	{
		//////////////////////////////////////////////////////////////
		// Events
		//////////////////////////////////////////////////////////////
		// notify when a Player is attached or un-attached 
		public delegate void OnPlayerAttached(Actor actor, Actor last);
		public OnPlayerAttached onPlayerAttached = null;
		
		//////////////////////////////////////////////////////////////
		// Input Processing and Velocity
		//////////////////////////////////////////////////////////////	
		// enum for mouse button assignment
		public enum Button 	 { Left, Right, Middle, None, Disabled }
		public enum TurnType { Button, Key }
		
		// mouse button index for primary actor control
		public Button actorControl			= Button.Right;
		
		// mouse button index for camera control
		public Button cameraControl			= Button.Left;
		
		// current movement velocity 
		public Vector3 velocity 			= Vector3.zero;
		
		// amount of angle (rotation) we're adding up each frame, to the actor
		public Vector3 angularVelocity		= Vector3.zero;
	
		// current mouse states: x = turn, y = look, z = zoom
		public Vector3 mouse 				= Vector3.zero;
		
		// how much modify current movement velocity if "walking"
		public float walkScale				= 0.5f;
		
		// our current main actor player
		protected Actor player 				= null;
		
		// flag to indicate that the InputManager contains a "Walk" definition
		protected bool walkInput			= true;
		
		// flag to indicate that the InputManager contains a "Strafe" definition
		protected bool sideInput			= true;
		
		// flag to indicate that the InputManager contains an "AutoMove" definition
		protected bool autoInput			= true;
		
		// current mouse button states
		protected bool buttonActor			= false;
		protected bool buttonCamera 		= false;
		
		// auto input helpers
		protected bool autoMove				= false;
		protected bool autoDown				= false;
		
		//////////////////////////////////////////////////////////////
		// Camera Control
		//////////////////////////////////////////////////////////////		
		// multiplied by mouse input, scrolling and keys (only when turning), essentially how fast we look, turn and zoom
		public Vector3 sensitivity			= new Vector3(10f, 10f, 5f);
		
		// each frame we decrease the look, turn and zoom by a small amount to decrease input momentum
		public Vector3 damping				= new Vector3(0.7f, 0.7f, 0.5f);
	
		// min/max we clamp to when looking up/down
		public Range lookRange				= new Range(-65f,  85f);
		
		// min/max we clamp to when zooming with the scroll wheel
		public Range zoomRange				= new Range(0.5f, 50f);
		
		// maximum mouse velocity we can achieve 
		public Vector3 maxMouseVelocity		= new Vector3(10f, 10f, 10f);
		
		// how fast the camera lerps between collision and the current zoom distance
		public float zoomLerpSpeed	 		= 10f;
	
		// amount of look (viewDelta.x) and turn (viewDelta.y) rotation, and zoom (viewDelta.z) distance, we're adding up each frame
		protected Vector3 viewDelta			= Vector3.zero;
		
		// actual look and turn angles we apply to the camera each frame (viewAngle.z is not set, unless you want to change the camera's up vector)
		protected Vector3 viewAngle 		= Vector3.zero;
		
		// this transform's current position + collider.height * eyeHeightScale
		protected Vector3 eyePosition		= Vector3.zero;
		
		// for convenience, the camera's RaycastHit data
		protected RaycastHit hitCamera		= new RaycastHit();
		
		// current target zoom distance we're moving toward
		protected float moveDistance		= 0f;
		
		// scroll wheel distance
		protected float zoomDistance		= 0f;
		
		// current actual view distance we're multiplying by the camera's rotation
		protected float viewDistance		= 0f;	
		
		// last turn type, button or key
		protected TurnType turnType			= TurnType.Key;
		
		//////////////////////////////////////////////////////////////
		// Constant Variables
		//////////////////////////////////////////////////////////////
		protected const float MASS_SCALE	= 0.05f;		
		
		//////////////////////////////////////////////////////////////
		// Set Player 
		//////////////////////////////////////////////////////////////
		// sets the current player and makes the callback
		public virtual Actor Player
		{
			get { return player; }
			set 
			{
				// if we're already us, go no further
				if (player == value) return;
				
				// notify everybody of the change
				if (onPlayerAttached != null) onPlayerAttached(value, player);
				
				// set the reference to the new player
				player = value;
				
				// default distance behind the player is the player's current Renderer's bounding diameter
				if (player != null) 
				{
					zoomDistance = viewDistance	= moveDistance = player.RendererBoundingRadius * 2f;	
					Reset();
				}
			}
		}
		// reset some of the more important variables as well as the camera angles
		public virtual void Reset()
		{
			autoDown		= autoMove = false;
			viewAngle 		= new Vector3(20f, 0f, 0f);
			velocity 		= Vector3.zero;
			angularVelocity = Vector3.zero;
			GetComponent<Camera>().transform.eulerAngles = viewAngle;
		}
		
		//////////////////////////////////////////////////////////////
		// Init 
		//////////////////////////////////////////////////////////////
		protected virtual void Awake()
		{
			Instance = this;
		}
		protected virtual void Start()
		{
			// check for definitions in InputManager -- use defaults if not present
			try { Input.GetAxis("Walk");     } 
			catch (Exception e) { Debug.Log("InputManager does not support WALK key, using L/R SHIFT" + ": " 	+ e.Message); walkInput = false; }
			try { Input.GetAxis("Strafe");   } 
			catch (Exception e) { Debug.Log("InputManager does not support STAFE keys, using Q/E" + ": "       	+ e.Message); sideInput = false; }
			try { Input.GetAxis("AutoMove"); } 
			catch (Exception e) { Debug.Log("InputManager does not support AUTOMOVE key, using PAGEUP" + ": "   + e.Message); autoInput = false; }
		}
		
		//////////////////////////////////////////////////////////////
		// Input Control 
		//////////////////////////////////////////////////////////////
		bool MapButton(Button button)
		{
			if (button == Button.None) 		return true;
			if (button == Button.Disabled) 	return false;
			return Input.GetMouseButton((int)button);
		}
		protected virtual void Update()
		{
			if (player == null)
			{
				Debug.Log("KAM3RA does not have a valid Player!");
				return;
			}
			
			// GetAxis is supposed to be fps-independent, however it seems 
			// to be somewhat inconsistent with mouse movement
			float FPSADJ = Time.deltaTime * 50f;
			
			// button assignments -- this could be refactored to use InputManager definitions 
			// however InputManager can't be changed at runtime, so we use our own here
			buttonActor 	= MapButton(actorControl);
			buttonCamera 	= MapButton(cameraControl);

			// mouse input
			mouse.x			= Input.GetAxis("Mouse X") 				* FPSADJ;
			mouse.y			= Input.GetAxis("Mouse Y") 				* FPSADJ;
			mouse.z			= Input.GetAxis("Mouse ScrollWheel") 	* FPSADJ * 2f; 
			
			// back < 0, forward > 0
			velocity.z 		= Normalize(Input.GetAxis("Vertical")); 
			
			// left < 0, right > 0
			velocity.x  	= Normalize(Input.GetAxis("Horizontal"));

			// up > 0, down N/A
			velocity.y 		= Input.GetAxis("Jump") != 0f ? 1f : 0f; 
			
			// is the user holding down a velocity modification key?
			velocity 		*= walkInput ? (Input.GetAxis("Walk") != 0 ? walkScale : 1f) : (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? walkScale : 1f);
			
			// if on this mouse button, increment turning on the y rotation axis
			if (buttonActor) 
			{
				turnType = TurnType.Button;
				angularVelocity.y = Mathf.Clamp(angularVelocity.y + mouse.x * sensitivity.y, -maxMouseVelocity.y, maxMouseVelocity.y);
			}
			// the user is using the keys to execute a turn -- use the current left/right key state then set velocity.x to explicit strafeing
			else 
			{
				// back off sensitivity if on keys, also if backing up with keys, switch directions
				turnType = TurnType.Key;
				if (velocity.x != 0) 
				{
					angularVelocity.y = Input.GetAxis("Horizontal");
					angularVelocity.y *= sensitivity.y * (velocity.z < 0 ? -1f : 1f) * FPSADJ * 2f;
				}
				velocity.x = sideInput ? Normalize(Input.GetAxis("Strafe")) : (Input.GetKey(KeyCode.Q) ? -1 : (Input.GetKey(KeyCode.E) ? 1 : 0));
			}
			
			// apply damping to the current turn amount, every frame -- this slows down the momentum created above
			angularVelocity = Vector3.Scale(angularVelocity, damping * (turnType == TurnType.Key ? 0.25f : 1f));	

			// auto move -- forward only
			bool auto = (autoInput ? Normalize(Input.GetAxis("AutoMove")) : (Input.GetKey(KeyCode.PageUp) ? 1 : 0)) > 0;
			if (!autoDown && auto) autoMove = !autoMove; autoDown = auto;
			if (autoMove) { if (velocity.x != 0 || velocity.z != 0) autoMove = false; else { velocity.x = 0f; velocity.z = 1f; } }

			// callback to the currently assigned player
			player.UserUpdate(this);	
			
		}
		protected virtual void LateUpdate() 
		{
			if (player == null) return;
	
			// viewDelta X is the look up/down rotation
			if (buttonActor || buttonCamera) 
				viewDelta.x = Mathf.Clamp(viewDelta.x + mouse.y * sensitivity.x, -maxMouseVelocity.y, maxMouseVelocity.y);
			
			// viewDelta Y is the look left/right rotation
			if (buttonCamera) 
				viewDelta.y = Mathf.Clamp(viewDelta.y + mouse.x * sensitivity.y, -maxMouseVelocity.x, maxMouseVelocity.x);
			
			// viewDelta Z is the zoom in/out distance
			if (Scrolling) 
				viewDelta.z = Mathf.Clamp(mouse.z * sensitivity.z, -maxMouseVelocity.z, maxMouseVelocity.z); 
			
			// apply damping every frame
			viewDelta = Vector3.Scale(viewDelta, damping);		
			
			// increment the actual angle up/down -- clamp the value since we don't want to turn upside-down
			viewAngle.x = Mathf.Clamp(viewAngle.x - viewDelta.x, lookRange.min, lookRange.max);
			
			// increment the acutal angle left/right -- no clamp needed here
			viewAngle.y += viewDelta.y;
			
			// set the camera angle -- note that viewAngle.z is not set and is zero
			// viewAngle is the current camera angle + the angle of the transform 
			// since viewDelta.y is the left mouse button and angularVelocity.y (in Update()) 
			// is the right mouse button, this enables us to rotate around the actor
			GetComponent<Camera>().transform.eulerAngles = player.transform.eulerAngles + viewAngle;
			
			// compute the eye position -- a bit above where the actor's eyes are located usually works nicely
			// note that we're using collider.height here instead renderer.bounds.size.y, which would give choppy 
			// results since the calculated Renderer bounds will move around a lot... hence we must multiply by scale
			eyePosition = player.EyePosition;
			
			// zoom based on the viewDelta.z value above, clamping to min/max zoom
			// we set both zoomDistance and moveDistance here since they are the same unless there is a collision
			zoomDistance = moveDistance = Mathf.Clamp(zoomDistance - viewDelta.z, zoomRange.min, zoomRange.max);	
			
			// rate at which we lerp the camera 
			float speedDistance = Time.deltaTime * zoomLerpSpeed;
			
			// now check for a camera collision; if a collision, set a new distance clamped to the current zoom distance
			// note that speedDistance is also modified according to the speed of input to ensure the distance lerp keeps up with the user's twitchiness
			RaycastHit[] hits   = Physics.SphereCastAll(player.WaistPosition, player.Radius * 0.5f, -GetComponent<Camera>().transform.forward, zoomDistance);
			hitCamera.distance  = Mathf.Infinity;
			foreach (RaycastHit hit in hits)
			{
				if (hit.distance < hitCamera.distance && hit.transform != player.transform)
				{
					bool found = false;
					if (player.transform.childCount > 0)
					{
						foreach (Transform t in player.transform)
						{
							if (hit.transform == t)
							{
								found = true;
								break;
							}
						}
					}
					if (!found) hitCamera = hit;
				}
			}
			if (hitCamera.distance != Mathf.Infinity)
			{
				moveDistance  = Mathf.Min(Vector3.Distance(hitCamera.point, eyePosition), zoomDistance);
				speedDistance += Mathf.Max(Mathf.Abs(viewDelta.x), Math.Abs(viewDelta.y));
			}
			
			// continually lerp to final distance
			viewDistance = Mathf.Lerp(viewDistance, moveDistance, speedDistance);
			
			// actor's eye position in world space minus the camera's normal scaled by the view distance
			GetComponent<Camera>().transform.position = eyePosition - GetComponent<Camera>().transform.forward * viewDistance;
			
			// if we hit close to min zoom, hide the chararacter
			player.SetEnabled(Mathf.Abs(viewDistance - zoomRange.min) > 0.1f);
		}	
		
		//////////////////////////////////////////////////////////////
		// Helpers
		//////////////////////////////////////////////////////////////
		// direction of the *actor* -- this is viewAngle without viewAngle.y
		// so we grab the camera temporarily, strip viewAngle.y and restore
		public Vector3 Direction
		{
			get 
			{ 
				if (player == null) return GetComponent<Camera>().transform.forward;
				Transform t 	= GetComponent<Camera>().transform;
				Vector3 e 		= t.eulerAngles;
				Vector3 p 		= SetVectorY(viewAngle, 0f);
				t.eulerAngles 	= player.transform.eulerAngles + p;
				p 				= t.forward;
				t.eulerAngles 	= e;
				return p;
			} 
		}
		
		// normalize to 0, 1 or -1
		public float Normalize(float value)
		{
			if (value > 0f) return  1f;
			if (value < 0f) return -1f;
			return 0f;
		}
		
		// returns true if the user is scrolling the mouse wheel
		public bool Scrolling  		{ get { return mouse.z != 0; 						} }
		
		// returns true if the user is turning and not using the button indicated by CharacterControl
		public bool Turning   		{ get { return angularVelocity.x != 0; 				} }
		
		// returns true if forward/back or side movement
		public bool Moving			{ get { return velocity.x != 0 || velocity.z != 0; 	} }
		// individual movements
		public bool MovingForward	{ get { return velocity.z > 0; 						} }
		public bool MovingBack		{ get { return velocity.z < 0; 						} }
		public bool MovingRight		{ get { return velocity.x < 0; 						} }
		public bool MovingLeft		{ get { return velocity.x > 0; 						} }
		
		// returns true if jumping
		public bool Jumping			{ get { return velocity.y != 0; 					} }

		// is this Player attached?
		public bool Attached(Actor player) { return this.player == player; }	
		
		// status string with important info
		public string Status
		{
			get 
			{ 
				return "Velocity: " + velocity.ToString() + "\n" +
					   "Angular: "	+ angularVelocity.ToString() + "\n" +
					   "Mouse: " 	+ mouse.ToString(); 
			}
		}
		
		//////////////////////////////////////////////////////////////
		// Static
		//////////////////////////////////////////////////////////////
		public static User Instance = null;
		public static Vector3 SetVectorX(Vector3 v, float x) 	{ v.x  = x; return v; }
		public static Vector3 SetVectorY(Vector3 v, float y) 	{ v.y  = y; return v; }
		public static Vector3 SetVectorZ(Vector3 v, float z) 	{ v.z  = z; return v; }
		public static Vector3 SetVectorXZ(Vector3 v, float x, float z) 	{ v.x = x; v.z  = z; return v; }
		public static Vector3 AddVectorX(Vector3 v, float x) 	{ v.x += x; return v; }
		public static Vector3 AddVectorY(Vector3 v, float y) 	{ v.y += y; return v; }
		public static Vector3 AddVectorZ(Vector3 v, float z) 	{ v.z += z; return v; }
		public static Vector3 VectorXZ(Vector3 v)   			{ return new Vector3(v.x, 0f, v.z); }
		public static float Distance2D(Vector3 v1, Vector2 v2) 	{ v1.y = v2.y = 0; return Vector3.Distance(v1, v2); }
		public static Vector3 MakeUniformScale(float scale)		{ return new Vector3(scale, scale, scale); }
		public static void LookAt2D(Transform transform, Vector3 point) 
		{ 
			Vector3 e = transform.eulerAngles; 
			transform.LookAt(point); 
			e.y = transform.eulerAngles.y; 
			transform.eulerAngles = e;
		}
		public static float CalculateMass(float radius)
		{
			 return Mathf.Min(radius * MASS_SCALE, 10f);
		}
		// for setting paths while walking
		private static List<Vector3>positions = new List<Vector3>();
		public static void ClearPrintPositions()
		{
			positions.Clear();
		}
		public static void PrintPositions(Transform transform)
		{
			// editor-only
			if (!Application.isPlaying && Input.GetKeyDown(KeyCode.C))
			{
				foreach (Vector3 m in positions) if (m == transform.position) return;
				positions.Add(transform.position);
				string all = "";
				foreach (Vector3 m in positions) all += m.x + ", " + m.y + ", " + m.z + "\n";
				Debug.Log(all);
			}
		}
	}
}