using System.Collections.Generic;
using UnityEngine;

namespace Raygeas
{
    public class PlayerController : MonoBehaviour
    {
        [System.Serializable]
        public class GroundLayer
        {
            public string layerName;
            public Texture2D[] groundTextures;
            public AudioClip[] footstepSounds;
        }

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5.0f;
        [SerializeField] private float runMultiplier = 3.0f;
        [SerializeField] private float jumpForce = 5.0f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Mouse Look")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float mouseSensivity = 1.0f;
        [SerializeField] private float mouseVerticalClamp = 90.0f;

        [Header("Keybinds")]
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode runKey = KeyCode.LeftShift;

        [Header("Footsteps")]
        [SerializeField] private AudioSource footstepSource;
        [SerializeField][Range(0f, 1f)] private float walkFootstepVolume = 0.4f;
        [SerializeField][Range(0f, 1f)] private float runFootstepVolume = 0.8f;
        [SerializeField] private float groundCheckDistance = 1.5f;
        [SerializeField][Range(1f, 4f)] private float footstepRate = 1f;
        [SerializeField][Range(1f, 4f)] private float runningFootstepRate = 1.5f;

        public List<GroundLayer> groundLayers = new List<GroundLayer>();

        private float _horizontalMovement;
        private float _verticalMovement;
        private float _currentSpeed;
        private Vector3 _moveDirection;
        private Vector3 _velocity;
        private CharacterController _characterController;
        private bool _isRunning;

        private float _verticalRotation;
        private float _yAxis;
        private float _xAxis;

        private Terrain[] _terrains;
        private Dictionary<Terrain, TerrainData> _terrainDataMap = new();
        private Dictionary<Terrain, TerrainLayer[]> _terrainLayersMap = new();
        private AudioClip _previousClip;
        private Texture2D _currentTexture;
        private RaycastHit _groundHit;
        private float _nextFootstep;

        private void Awake()
        {
            GetTerrainData();
            _characterController = GetComponent<CharacterController>();
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void GetTerrainData()
        {
            _terrains = Terrain.activeTerrains;
            foreach (Terrain terrain in _terrains)
            {
                _terrainDataMap[terrain] = terrain.terrainData;
                _terrainLayersMap[terrain] = terrain.terrainData.terrainLayers;
            }
        }

        private void Update()
        {
            Movement();
            MouseLook();
            // GroundChecker(); ❌ מבוטל בגלל שגיאה עם Synty
        }

        private void FixedUpdate()
        {
            SpeedCheck();
        }

        private void Movement()
        {
            if (_characterController.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            if (Input.GetKey(jumpKey) && _characterController.isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }

            _horizontalMovement = Input.GetAxis("Horizontal");
            _verticalMovement = Input.GetAxis("Vertical");

            _moveDirection = transform.forward * _verticalMovement + transform.right * _horizontalMovement;

            _isRunning = Input.GetKey(runKey);
            _currentSpeed = walkSpeed * (_isRunning ? runMultiplier : 1f);
            _characterController.Move(_moveDirection * _currentSpeed * Time.deltaTime);

            _velocity.y += gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void MouseLook()
        {
            _xAxis = Input.GetAxis("Mouse X");
            _yAxis = Input.GetAxis("Mouse Y");

            _verticalRotation += -_yAxis * mouseSensivity;
            _verticalRotation = Mathf.Clamp(_verticalRotation, -mouseVerticalClamp, mouseVerticalClamp);
            playerCamera.transform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);
            transform.rotation *= Quaternion.Euler(0, _xAxis * mouseSensivity, 0);
        }

        private void SpeedCheck()
        {
            if (_characterController.isGrounded && (_horizontalMovement != 0 || _verticalMovement != 0))
            {
                float currentFootstepRate = (_isRunning ? runningFootstepRate : footstepRate);

                if (_nextFootstep >= 100f)
                {
                    PlayFootstep();
                    _nextFootstep = 0;
                }
                _nextFootstep += (currentFootstepRate * walkSpeed);
            }
        }

        private void PlayFootstep()
        {
            for (int i = 0; i < groundLayers.Count; i++)
            {
                for (int k = 0; k < groundLayers[i].groundTextures.Length; k++)
                {
                    if (_currentTexture == groundLayers[i].groundTextures[k])
                    {
                        footstepSource.PlayOneShot(RandomClip(groundLayers[i].footstepSounds));
                    }
                }
            }
        }

        private AudioClip RandomClip(AudioClip[] clips)
        {
            int attempts = 2;
            footstepSource.pitch = Random.Range(0.9f, 1.1f);

            footstepSource.volume = _isRunning ? runFootstepVolume : walkFootstepVolume;

            AudioClip selectedClip = clips[Random.Range(0, clips.Length)];

            while (selectedClip == _previousClip && attempts > 0)
            {
                selectedClip = clips[Random.Range(0, clips.Length)];
                attempts--;
            }

            _previousClip = selectedClip;
            return selectedClip;
        }
    }
}