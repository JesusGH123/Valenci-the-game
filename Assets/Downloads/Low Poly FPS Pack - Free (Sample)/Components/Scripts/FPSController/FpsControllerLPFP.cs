using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace FPSControllerLPFP
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(AudioSource))]
    public class FpsControllerLPFP : MonoBehaviour
    {
#pragma warning disable 649
        [Header("Arms")]
        [Tooltip("The transform component that holds the gun camera."), SerializeField]
        private Transform arms;

        [Tooltip("The position of the arms and gun camera relative to the fps controller GameObject."), SerializeField]
        private Vector3 armPosition;

        [Header("Audio Clips")]
        [Tooltip("The audio clip that is played while walking."), SerializeField]
        private AudioClip walkingSound;

        [Tooltip("The audio clip that is played while running."), SerializeField]
        private AudioClip runningSound;

        [Tooltip("The audio clip that is played while collecting boxes."), SerializeField]
        private AudioClip collectBoxSound;

        [Header("Movement Settings")]
        [Tooltip("How fast the player moves while walking and strafing."), SerializeField]
        private float walkingSpeed = 5f;

        [Tooltip("How fast the player moves while running."), SerializeField]
        private float runningSpeed = 9f;

        [Tooltip("Approximately the amount of time it will take for the player to reach maximum running or walking speed."), SerializeField]
        private float movementSmoothness = 0.125f;

        [Tooltip("Amount of force applied to the player when jumping."), SerializeField]
        private float jumpForce = 35f;

        [Header("Look Settings")]
        [Tooltip("Rotation speed of the fps controller."), SerializeField]
        private float mouseSensitivity = 7f;

        [Tooltip("Approximately the amount of time it will take for the fps controller to reach maximum rotation speed."), SerializeField]
        private float rotationSmoothness = 0.05f;

        [Tooltip("Minimum rotation of the arms and camera on the x axis."),
         SerializeField]
        private float minVerticalAngle = -90f;

        [Tooltip("Maximum rotation of the arms and camera on the axis."),
         SerializeField]
        private float maxVerticalAngle = 90f;

        [Tooltip("The names of the axes and buttons for Unity's Input Manager."), SerializeField]
        private FpsInput input;
#pragma warning restore 649
        //[Header("Audio Source")]
        //public AudioSource collectAmmoAudioSource;

        //[Serializable]
        //public class soundClips
        //{
        //    public AudioClip collectAmmoSound;
        //}
        //public soundClips SoundClips;

        [SerializeField]
        private int Playerhealth = 10;
        private int PlayerAmmo = 60;
        private bool immunity = false;
        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;
        private AudioSource _audioSource;
        private SmoothRotation _rotationX;
        private SmoothRotation _rotationY;
        private SmoothVelocity _velocityX;
        private SmoothVelocity _velocityZ;
        private bool _isGrounded;

        private readonly RaycastHit[] _groundCastResults = new RaycastHit[8];
        private readonly RaycastHit[] _wallCastResults = new RaycastHit[8];

        public GameObject ammoBox;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            _collider = GetComponent<CapsuleCollider>();
            _audioSource = GetComponent<AudioSource>();
            arms = AssignCharactersCamera();
            _audioSource.clip = walkingSound;
            _audioSource.loop = true;
            _rotationX = new SmoothRotation(RotationXRaw);
            _rotationY = new SmoothRotation(RotationYRaw);
            _velocityX = new SmoothVelocity();
            _velocityZ = new SmoothVelocity();
            Cursor.lockState = CursorLockMode.Locked;
            ValidateRotationRestriction();
            //collectAmmoAudioSource.clip = SoundClips.collectAmmoSound;
        }

        private Transform AssignCharactersCamera()
        {
            var t = transform;
            arms.SetPositionAndRotation(t.position, t.rotation);
            return arms;
        }

        private void ValidateRotationRestriction()
        {
            minVerticalAngle = ClampRotationRestriction(minVerticalAngle, -90, 90);
            maxVerticalAngle = ClampRotationRestriction(maxVerticalAngle, -90, 90);
            if (maxVerticalAngle >= minVerticalAngle) return;
            Debug.LogWarning("maxVerticalAngle should be greater than minVerticalAngle.");
            var min = minVerticalAngle;
            minVerticalAngle = maxVerticalAngle;
            maxVerticalAngle = min;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.tag == "MedBox")
            {
                Destroy(other.gameObject);
            }

            if (other.tag == "AmmoBox")
            {
                //other.GetComponent<AutomaticGunScriptLPFP>().SoundClips.reloadSoundAmmoLeft;
                //collectAmmoAudioSource.clip = SoundClips.collectAmmoSound;
                //collectAmmoAudioSource.Play();
                PlayerAmmo += 90;
                Destroy(other.gameObject);
            }
        }

        private static float ClampRotationRestriction(float rotationRestriction, float min, float max)
        {
            if (rotationRestriction >= min && rotationRestriction <= max) return rotationRestriction;
            var message = string.Format("Rotation restrictions should be between {0} and {1} degrees.", min, max);
            Debug.LogWarning(message);
            return Mathf.Clamp(rotationRestriction, min, max);
        }

        private void OnCollisionStay(Collision collision)
        {
            var bounds = _collider.bounds;
            var extents = bounds.extents;
            var radius = extents.x - 0.01f;
            Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down, _groundCastResults, extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);
            if (!_groundCastResults.Any(hit => hit.collider != null && hit.collider != _collider)) return;
            for (var i = 0; i < _groundCastResults.Length; i++)
            {
                _groundCastResults[i] = new RaycastHit();
            }

            _isGrounded = true;

            if (collision.gameObject.tag == "Enemy" && !immunity)
            {
                StartCoroutine("characterDamage", 1);
            }
        }


        private void FixedUpdate()
        {
            RotateCameraAndCharacter();
            MoveCharacter();
            _isGrounded = false;
        }

        private void Update()
        {
            if (Playerhealth == 0)        //Player death
                SceneManager.LoadScene("GameOver");    //For now, we are destroying the player (we have to change this later)

            if(Input.GetKeyDown(KeyCode.R) && PlayerAmmo > 0 )
            {
                StartCoroutine("Reload");
            }

            arms.position = transform.position + transform.TransformVector(armPosition);
            Jump();
            PlayFootstepSounds();

            //Knife raycast
            int enemyLayer = 1 << 6;
            RaycastHit hit;

            if (Input.GetKeyDown(KeyCode.F))
            {
                if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 1f, enemyLayer))
                    hit.transform.gameObject.GetComponent<ZombieBehaviour>().ReceiveDamage(3);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                //Wait for the animation
                if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 1f, enemyLayer))
                    StartCoroutine("WaitKnife", hit);
            }
        }

        private void RotateCameraAndCharacter()
        {
            var rotationX = _rotationX.Update(RotationXRaw, rotationSmoothness);
            var rotationY = _rotationY.Update(RotationYRaw, rotationSmoothness);
            var clampedY = RestrictVerticalRotation(rotationY);
            _rotationY.Current = clampedY;
            var worldUp = arms.InverseTransformDirection(Vector3.up);
            var rotation = arms.rotation *
                           Quaternion.AngleAxis(rotationX, worldUp) *
                           Quaternion.AngleAxis(clampedY, Vector3.left);
            transform.eulerAngles = new Vector3(0f, rotation.eulerAngles.y, 0f);
            arms.rotation = rotation;
        }

        private float RotationXRaw
        {
            get { return input.RotateX * mouseSensitivity; }
        }

        private float RotationYRaw
        {
            get { return input.RotateY * mouseSensitivity; }
        }

        private float RestrictVerticalRotation(float mouseY)
        {
            var currentAngle = NormalizeAngle(arms.eulerAngles.x);
            var minY = minVerticalAngle + currentAngle;
            var maxY = maxVerticalAngle + currentAngle;
            return Mathf.Clamp(mouseY, minY + 0.01f, maxY - 0.01f);
        }

        private static float NormalizeAngle(float angleDegrees)
        {
            while (angleDegrees > 180f)
            {
                angleDegrees -= 360f;
            }

            while (angleDegrees <= -180f)
            {
                angleDegrees += 360f;
            }

            return angleDegrees;
        }

        private void MoveCharacter()
        {
            var direction = new Vector3(input.Move, 0f, input.Strafe).normalized;
            var worldDirection = transform.TransformDirection(direction);
            var velocity = worldDirection * (input.Run ? runningSpeed : walkingSpeed);
            //Checks for collisions so that the character does not stuck when jumping against walls.
            var intersectsWall = CheckCollisionsWithWalls(velocity);
            if (intersectsWall)
            {
                _velocityX.Current = _velocityZ.Current = 0f;
                return;
            }

            var smoothX = _velocityX.Update(velocity.x, movementSmoothness);
            var smoothZ = _velocityZ.Update(velocity.z, movementSmoothness);
            var rigidbodyVelocity = _rigidbody.velocity;
            var force = new Vector3(smoothX - rigidbodyVelocity.x, 0f, smoothZ - rigidbodyVelocity.z);
            _rigidbody.AddForce(force, ForceMode.VelocityChange);
        }

        private bool CheckCollisionsWithWalls(Vector3 velocity)
        {
            if (_isGrounded) return false;
            var bounds = _collider.bounds;
            var radius = _collider.radius;
            var halfHeight = _collider.height * 0.5f - radius * 1.0f;
            var point1 = bounds.center;
            point1.y += halfHeight;
            var point2 = bounds.center;
            point2.y -= halfHeight;
            Physics.CapsuleCastNonAlloc(point1, point2, radius, velocity.normalized, _wallCastResults,
                radius * 0.04f, ~0, QueryTriggerInteraction.Ignore);
            var collides = _wallCastResults.Any(hit => hit.collider != null && hit.collider != _collider);
            if (!collides) return false;
            for (var i = 0; i < _wallCastResults.Length; i++)
            {
                _wallCastResults[i] = new RaycastHit();
            }

            return true;
        }

        private void Jump()
        {
            if (!_isGrounded || !input.Jump) return;
            _isGrounded = false;
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        private void PlayFootstepSounds()
        {
            if (_isGrounded && _rigidbody.velocity.sqrMagnitude > 0.1f)
            {
                _audioSource.clip = input.Run ? runningSound : walkingSound;
                if (!_audioSource.isPlaying)
                {
                    _audioSource.Play();
                }
            }
            else
            {
                if (_audioSource.isPlaying)
                {
                    _audioSource.Pause();
                }
            }
        }

        private class SmoothRotation
        {
            private float _current;
            private float _currentVelocity;

            public SmoothRotation(float startAngle)
            {
                _current = startAngle;
            }

            public float Update(float target, float smoothTime)
            {
                return _current = Mathf.SmoothDampAngle(_current, target, ref _currentVelocity, smoothTime);
            }

            public float Current
            {
                set { _current = value; }
            }
        }

        private class SmoothVelocity
        {
            private float _current;
            private float _currentVelocity;

            public float Update(float target, float smoothTime)
            {
                return _current = Mathf.SmoothDamp(_current, target, ref _currentVelocity, smoothTime);
            }

            public float Current
            {
                set { _current = value; }
            }
        }

        [Serializable]
        private class FpsInput
        {
            [Tooltip("The name of the virtual axis mapped to rotate the camera around the y axis."),
             SerializeField]
            private string rotateX = "Mouse X";

            [Tooltip("The name of the virtual axis mapped to rotate the camera around the x axis."),
             SerializeField]
            private string rotateY = "Mouse Y";

            [Tooltip("The name of the virtual axis mapped to move the character back and forth."),
             SerializeField]
            private string move = "Horizontal";

            [Tooltip("The name of the virtual axis mapped to move the character left and right."),
             SerializeField]
            private string strafe = "Vertical";

            [Tooltip("The name of the virtual button mapped to run."),
             SerializeField]
            private string run = "Fire3";

            [Tooltip("The name of the virtual button mapped to jump."),
             SerializeField]
            private string jump = "Jump";

            public float RotateX
            {
                get { return Input.GetAxisRaw(rotateX); }
            }

            public float RotateY
            {
                get { return Input.GetAxisRaw(rotateY); }
            }

            public float Move
            {
                get { return Input.GetAxisRaw(move); }
            }

            public float Strafe
            {
                get { return Input.GetAxisRaw(strafe); }
            }

            public bool Run
            {
                get { return Input.GetButton(run); }
            }

            public bool Jump
            {
                get { return Input.GetButtonDown(jump); }
            }
        }

        IEnumerator characterDamage(int damage = 1)
        {
            Playerhealth-= damage;
            immunity = true;
            yield return new WaitForSeconds(3.0f);
            immunity = false;
        }

        IEnumerator WaitKnife(RaycastHit hit)
        {
            yield return new WaitForSeconds(1.0f);
            hit.transform.gameObject.GetComponent<ZombieBehaviour>().ReceiveDamage(3);
        }

        IEnumerator Reload()
        {
            PlayerAmmo-=30;
            yield return null;
        }

        public int getLife()
        {
            return Playerhealth;
        }

        public int getAmmo()
        {
            return PlayerAmmo;
        }

    }
}