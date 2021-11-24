using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using FPSControllerLPFP;

public class AutomaticGunScriptLPFP : MonoBehaviour {

	Animator anim;

	[Header("Gun Camera")]

	public Camera gunCamera;
	
	[Header("Gun Camera Options")]
	[Tooltip("How fast the camera field of view changes when aiming.")]
	public float fovSpeed = 15.0f;

	[Tooltip("Default value for camera field of view (40 is recommended).")]
	public float defaultFov = 40.0f;

	public float aimFov = 25.0f;

	[Header("UI Weapon Name")]
	[Tooltip("Name of the current weapon, shown in the game UI.")]
	public string weaponName;
	private string storedWeaponName;

	[Header("Weapon Sway")]

	[Tooltip("Toggle weapon sway.")]
	public bool weaponSway;

	public float swayAmount = 0.02f;
	public float maxSwayAmount = 0.06f;
	public float swaySmoothValue = 4.0f;

	private Vector3 initialSwayPosition;

	//Used for fire rate
	private float lastFired;
	[Header("Weapon Settings")]
	//How fast the weapon fires, higher value means faster rate of fire
	[Tooltip("How fast the weapon fires, higher value means faster rate of fire.")]
	public float fireRate;
	//Eanbles auto reloading when out of ammo
	[Tooltip("Enables auto reloading when out of ammo.")]
	public bool autoReload;
	//Delay between shooting last bullet and reloading
	public float autoReloadDelay;
	//Check if reloading
	private bool isReloading;

	//Holstering weapon
	private bool hasBeenHolstered = false;
	//If weapon is holstered
	private bool holstered;
	//Check if running
	private bool isRunning;
	//Check if aiming
	private bool isAiming;
	//Check if walking
	private bool isWalking;
	//Check if inspecting weapon
	private bool isInspecting;

	//How much ammo is currently left
	private int currentAmmo;
	//Totalt amount of ammo
	[Tooltip("How much ammo the weapon should have.")]
	public int ammo;
	public int ammoPlayer;

	[Header("Health Settings")]
	[Tooltip("How much health.")]
	public int health;

	//Check if out of ammo
	private bool outOfAmmo;

	[Header("Bullet Settings")]
	//Bullet
	[Tooltip("How much force is applied to the bullet when shooting.")]
	public float bulletForce = 400.0f;
	[Tooltip("How long after reloading that the bullet model becomes visible " +
		"again, only used for out of ammo reload animations.")]
	public float showBulletInMagDelay = 0.6f;
	[Tooltip("The bullet model inside the mag, not used for all weapons.")]
	public SkinnedMeshRenderer bulletInMagRenderer;

	[Header("Grenade Settings")]
	public float grenadeSpawnDelay = 0.35f;
	public GameObject controller;

	[Header("Muzzleflash Settings")]
	public bool randomMuzzleflash = false;
	//min should always bee 1
	private int minRandomValue = 1;

	[Range(2, 25)]
	public int maxRandomValue = 5;

	private int randomMuzzleflashValue;

	public bool enableMuzzleflash = true;
	public ParticleSystem muzzleParticles;
	public bool enableSparks = true;
	public ParticleSystem sparkParticles;
	public int minSparkEmission = 1;
	public int maxSparkEmission = 7;

	[Header("Muzzleflash Light Settings")]
	public Light muzzleflashLight;
	public float lightDuration = 0.02f;

	[Header("Audio Source")]
	//Main audio source
	public AudioSource mainAudioSource;
	//Audio source used for shoot sound
	public AudioSource shootAudioSource;

	[Header("UI Components")]
	public Text timescaleText;
	public Text currentWeaponText;
	public Text currentAmmoText;
	public Text totalAmmoText;
	public Text currentHealthText;

	public Button pauseBtn;

	[System.Serializable]
	public class prefabs
	{  
		[Header("Prefabs")]
		public Transform bulletPrefab;
		public Transform casingPrefab;
		public Transform grenadePrefab;
	}
	public prefabs Prefabs;
	
	[System.Serializable]
	public class spawnpoints
	{  
		[Header("Spawnpoints")]
		//Array holding casing spawn points 
		//(some weapons use more than one casing spawn)
		//Casing spawn point array
		public Transform casingSpawnPoint;
		//Bullet prefab spawn from this point
		public Transform bulletSpawnPoint;

		public Transform grenadeSpawnPoint;
	}
	public spawnpoints Spawnpoints;

	[System.Serializable]
	public class soundClips
	{
		public AudioClip shootSound;
		public AudioClip takeOutSound;
		public AudioClip holsterSound;
		public AudioClip reloadSoundOutOfAmmo;
		public AudioClip reloadSoundAmmoLeft;
		public AudioClip aimSound;
	}
	public soundClips SoundClips;

	private bool soundHasPlayed = false;

	private void Awake () {
		
		anim = GetComponent<Animator>();
		currentAmmo = ammo;

		muzzleflashLight.enabled = false;
	}

	private void Start () {
		
		storedWeaponName = weaponName;
		currentWeaponText.text = weaponName;
		
		totalAmmoText.text = controller.GetComponent<FpsControllerLPFP>().getAmmo().ToString();

		initialSwayPosition = transform.localPosition;

		shootAudioSource.clip = SoundClips.shootSound;
	}

	private void LateUpdate () {
		
		if (weaponSway == true) 
		{
			float movementX = -Input.GetAxis ("Mouse X") * swayAmount;
			float movementY = -Input.GetAxis ("Mouse Y") * swayAmount;

			movementX = Mathf.Clamp 
				(movementX, -maxSwayAmount, maxSwayAmount);
			movementY = Mathf.Clamp 
				(movementY, -maxSwayAmount, maxSwayAmount);
			Vector3 finalSwayPosition = new Vector3 
				(movementX, movementY, 0);
			transform.localPosition = Vector3.Lerp 
				(transform.localPosition, finalSwayPosition + 
					initialSwayPosition, Time.deltaTime * swaySmoothValue);
		}
	}
	
	private void Update () {
		totalAmmoText.text = controller.GetComponent<FpsControllerLPFP>().getAmmo().ToString();

		if (Input.GetButton("Fire2") && !isReloading && !isRunning && !isInspecting) 
		{
			isAiming = true;

			anim.SetBool ("Aim", true);

			//When right click is released
			gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				aimFov,fovSpeed * Time.deltaTime);

			if (!soundHasPlayed) 
			{
				mainAudioSource.clip = SoundClips.aimSound;
				mainAudioSource.Play ();
	
				soundHasPlayed = true;
			}
		} 
		else 
		{
			gunCamera.fieldOfView = Mathf.Lerp(gunCamera.fieldOfView,
				defaultFov,fovSpeed * Time.deltaTime);

			isAiming = false;

			anim.SetBool ("Aim", false);
				
			soundHasPlayed = false;
		}

		if (randomMuzzleflash == true) 
		{
			randomMuzzleflashValue = Random.Range (minRandomValue, maxRandomValue);
		}

		//Pause game when 5 key is pressed
		if (Input.GetKeyDown (KeyCode.Alpha5)) 
		{
			Time.timeScale = 0.0f;
			timescaleText.text = "0.0";
		}

		//FPSControllerLPFP gameObj;
		//gameObj.GetComponent<FPSControllerLPFP>().g

		//Set current ammo text from ammo int
		currentAmmoText.text = currentAmmo.ToString ();
		//gameObject.GetComponent<FPSControllerLPFP
		//FpsControllerLPFP sn = gameObject.GetComponentInParent<FPSControllerLPFP>();
		
		currentHealthText.text = controller.GetComponent<FpsControllerLPFP>().getLife().ToString();

		AnimationCheck ();

		//Play knife attack 1 animation when Q key is pressed
		if (Input.GetKeyDown (KeyCode.Q) && !isInspecting) 
		{
			anim.Play ("Knife Attack 1", 0, 0f);
		}
		//Play knife attack 2 animation when F key is pressed
		if (Input.GetKeyDown (KeyCode.F) && !isInspecting) 
		{
			anim.Play ("Knife Attack 2", 0, 0f);
		}
			
		//Throw grenade when pressing G key
		if (Input.GetKeyDown (KeyCode.G) && !isInspecting) 
		{
			StartCoroutine (GrenadeSpawnDelay ());
			anim.Play("GrenadeThrow", 0, 0.0f);
		}

		//If out of ammo
		if (currentAmmo == 0) 
		{
			Debug.Log("Prueba balas -------------------------");
			Debug.Log(controller.GetComponent<FpsControllerLPFP>().getAmmo());
			currentWeaponText.text = "OUT OF AMMO";
			outOfAmmo = true;
			//Auto reload if true
			if (autoReload == true && !isReloading) 
			{
				StartCoroutine (AutoReload ());
			}
		} 
		else 
		{
			currentWeaponText.text = storedWeaponName.ToString ();
			outOfAmmo = false;
		}
			
		if (Input.GetMouseButton (0) && !outOfAmmo && !isReloading && !isInspecting && !isRunning) 
		{
			if (Time.time - lastFired > 1 / fireRate) 
			{
				lastFired = Time.time;

				currentAmmo -= 1;

				shootAudioSource.clip = SoundClips.shootSound;
				shootAudioSource.Play ();

				if (!isAiming)
				{
					anim.Play ("Fire", 0, 0f);
					if (!randomMuzzleflash && 
						enableMuzzleflash == true) 
					{
						muzzleParticles.Emit (1);
						StartCoroutine(MuzzleFlashLight());
					} 
					else if (randomMuzzleflash == true)
					{
						if (randomMuzzleflashValue == 1) 
						{
							if (enableSparks == true) 
							{
								sparkParticles.Emit (Random.Range (minSparkEmission, maxSparkEmission));
							}
							if (enableMuzzleflash == true) 
							{
								muzzleParticles.Emit (1);
								StartCoroutine (MuzzleFlashLight ());
							}
						}
					}
				} 
				else
				{
					
					anim.Play ("Aim Fire", 0, 0f);

					if (!randomMuzzleflash) {
						muzzleParticles.Emit (1);
					} 
					else if (randomMuzzleflash == true) 
					{
						if (randomMuzzleflashValue == 1) 
						{
							if (enableSparks == true) 
							{
								sparkParticles.Emit (Random.Range (minSparkEmission, maxSparkEmission));
							}
							if (enableMuzzleflash == true) 
							{
								muzzleParticles.Emit (1);
								StartCoroutine (MuzzleFlashLight ());
							}
						}
					}
				}

				var bullet = (Transform)Instantiate (
					Prefabs.bulletPrefab,
					Spawnpoints.bulletSpawnPoint.transform.position,
					Spawnpoints.bulletSpawnPoint.transform.rotation);

				bullet.GetComponent<Rigidbody>().velocity = 
					bullet.transform.forward * bulletForce;
				
				Instantiate (Prefabs.casingPrefab, 
					Spawnpoints.casingSpawnPoint.transform.position, 
					Spawnpoints.casingSpawnPoint.transform.rotation);
			}
		}

		//Inspect weapon when T key is pressed
		if (Input.GetKeyDown (KeyCode.T)) 
		{
			anim.SetTrigger ("Inspect");
		}

		//Toggle weapon holster when E key is pressed
		if (Input.GetKeyDown (KeyCode.E) && !hasBeenHolstered) 
		{
			holstered = true;

			mainAudioSource.clip = SoundClips.holsterSound;
			mainAudioSource.Play();

			hasBeenHolstered = true;
		} 
		else if (Input.GetKeyDown (KeyCode.E) && hasBeenHolstered) 
		{
			holstered = false;

			mainAudioSource.clip = SoundClips.takeOutSound;
			mainAudioSource.Play ();

			hasBeenHolstered = false;
		}

		if (holstered == true) 
		{
			anim.SetBool ("Holster", true);
		} 
		else 
		{
			anim.SetBool ("Holster", false);
		}

		if (Input.GetKeyDown (KeyCode.R) && !isReloading && !isInspecting) 
		{
			Reload ();
		}

		if (Input.GetKey (KeyCode.W) && !isRunning || 
			Input.GetKey (KeyCode.A) && !isRunning || 
			Input.GetKey (KeyCode.S) && !isRunning || 
			Input.GetKey (KeyCode.D) && !isRunning) 
		{
			anim.SetBool ("Walk", true);
		} else {
			anim.SetBool ("Walk", false);
		}

		if ((Input.GetKey (KeyCode.W) && Input.GetKey (KeyCode.LeftShift))) 
		{
			isRunning = true;
		} else {
			isRunning = false;
		}
		
		if (isRunning == true) 
		{
			anim.SetBool ("Run", true);
		} 
		else 
		{
			anim.SetBool ("Run", false);
		}
	}

	private IEnumerator GrenadeSpawnDelay () {
		
		yield return new WaitForSeconds (grenadeSpawnDelay);
		Instantiate(Prefabs.grenadePrefab, 
			Spawnpoints.grenadeSpawnPoint.transform.position, 
			Spawnpoints.grenadeSpawnPoint.transform.rotation);
	}

	private IEnumerator AutoReload () {
		yield return new WaitForSeconds (autoReloadDelay);
		if (controller.GetComponent<FpsControllerLPFP>().getAmmo() > 0)
		{
			if (outOfAmmo == true)
			{
				anim.Play("Reload Out Of Ammo", 0, 0f);

				mainAudioSource.clip = SoundClips.reloadSoundOutOfAmmo;
				mainAudioSource.Play();

				if (bulletInMagRenderer != null)
				{
					bulletInMagRenderer.GetComponent
					<SkinnedMeshRenderer>().enabled = false;
					StartCoroutine(ShowBulletInMag());
				}
			}
			//Restore ammo when reloading
			currentAmmo = ammo;
			ammo -= 1;
			outOfAmmo = false;
		}
	}

	private void Reload () {
		if (controller.GetComponent<FpsControllerLPFP>().getAmmo() > 0) {
			if (outOfAmmo == true) 
		{
			anim.Play ("Reload Out Of Ammo", 0, 0f);

			mainAudioSource.clip = SoundClips.reloadSoundOutOfAmmo;
			mainAudioSource.Play ();

			if (bulletInMagRenderer != null) 
			{
				bulletInMagRenderer.GetComponent
				<SkinnedMeshRenderer> ().enabled = false;
				StartCoroutine (ShowBulletInMag ());
			}
		} 
		else 
		{
			anim.Play ("Reload Ammo Left", 0, 0f);

			mainAudioSource.clip = SoundClips.reloadSoundAmmoLeft;
			mainAudioSource.Play ();

			if (bulletInMagRenderer != null) 
			{
				bulletInMagRenderer.GetComponent
				<SkinnedMeshRenderer> ().enabled = true;
			}
		}

			currentAmmo = ammo;
			outOfAmmo = false;
		}
	}

	private IEnumerator ShowBulletInMag () {
		
		yield return new WaitForSeconds (showBulletInMagDelay);
		bulletInMagRenderer.GetComponent<SkinnedMeshRenderer> ().enabled = true;
	}

	private IEnumerator MuzzleFlashLight () {
		
		muzzleflashLight.enabled = true;
		yield return new WaitForSeconds (lightDuration);
		muzzleflashLight.enabled = false;
	}

	private void AnimationCheck () {
		
		if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Reload Out Of Ammo") || 
			anim.GetCurrentAnimatorStateInfo (0).IsName ("Reload Ammo Left")) 
		{
			isReloading = true;
		} 
		else 
		{
			isReloading = false;
		}

		//Check if inspecting weapon
		if (anim.GetCurrentAnimatorStateInfo (0).IsName ("Inspect")) 
		{
			isInspecting = true;
		} 
		else 
		{
			isInspecting = false;
		}
	}
}