using System;
using TMPro;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    private const string TurnLeftParameter = "TurnLeft";
    private const string TurnRightParameter = "TurnRight";

    [Header("Déplacement")]
    [Tooltip("Vitesse du vaisseau, en unités par seconde")]
    [SerializeField] private float _moveSpeed = 8f;
    [Tooltip("Hauteur maximale du vaisseau, en unités du monde")]
    [SerializeField] private float _maxHeight = 0f;
    [SerializeField] private float _turnThreshold = 0.1f;

    [Header("Tir")]
    [SerializeField] private Laser _laserPrefab;
    [SerializeField] private Transform _firePoint;
    [Tooltip("Délai minimal entre deux tirs, en secondes")]
    [SerializeField] private float _fireDelay = 0.3f;

    [Header("Vies")]
    [Tooltip("Nombre de vies au début de la partie")]
    [SerializeField] private int _maxLives = 3;
    [Tooltip("Explosion créée à la destruction du vaisseau")]
    [SerializeField] private GameObject _explosionPrefab;



    /* BLOC 13-4.4 À ACTIVER : effacez cette ligne et la ligne de fin du bloc
    [Tooltip("Délai entre deux tirs pendant le tir rapide, en secondes")]
    [SerializeField] private float _rapidFireDelay = 0.1f;
    [Tooltip("Bouclier, enfant du joueur, affiché tant qu'il est actif")]
    [SerializeField] private GameObject _shieldVisual;
    FIN DU BLOC 13-4.4 : effacez cette ligne */

    private Rigidbody2D _rb;
    private GameInput _gameInput;
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private Vector2 _moveInput;
    private Vector2 _minPosition;
    private Vector2 _maxPosition;
    private float _nextFireTime;
    private bool _isFireHeld;
    private int _lives;
    /* BLOC 13-4.4 À ACTIVER : effacez cette ligne et la ligne de fin du bloc
    private float _currentFireDelay;
    private bool _hasShield;
    private Coroutine _rapidFireRoutine;
    FIN DU BLOC 13-4.4 : effacez cette ligne */

    /// <summary>
    /// Déclenché quand le nombre de vies change, avec les vies restantes.
    /// </summary>
    public static event Action<int> LivesChanged;

    /// <summary>
    /// Déclenché quand le joueur perd sa dernière vie.
    /// </summary>
    public static event Action Died;

    private void Awake()
    {
        _rb = GetComponentInChildren<Rigidbody2D>();
        // Un seul GameInput dans la scène : le code le trouve lui-même
        _gameInput = FindAnyObjectByType<GameInput>();

        _animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        _lives = _maxLives;
        /* BLOC 13-4.4 À ACTIVER : effacez cette ligne et la ligne de fin du bloc
        _currentFireDelay = _fireDelay;
        FIN DU BLOC 13-4.4 : effacez cette ligne */
    }

    private void OnEnable()
    {
        _gameInput.FirePressed += HandleFirePressed;
        _gameInput.FireReleased += HandleFireReleased;
    }

    private void OnDisable()
    {
        _gameInput.FirePressed -= HandleFirePressed;
        _gameInput.FireReleased -= HandleFireReleased;
    }

    private void Start()
    {
        ComputeLimits();

        // Tous les OnEnable de la scène ont eu lieu : le HUD est déjà abonné
        LivesChanged?.Invoke(_lives);
    }

    private void Update()
    {
        // Entrées lues à chaque image ; le déplacement attend FixedUpdate
        _moveInput = _gameInput.GetMovementVectorNormalized();
        

        _animator.SetBool(TurnLeftParameter, _moveInput.x < -_turnThreshold);
        _animator.SetBool(TurnRightParameter, _moveInput.x > _turnThreshold);

        // Tir continu tant que le bouton reste enfoncé
        if (_isFireHeld && Time.time >= _nextFireTime)
        {
            Fire();
        }
    }

    private void FixedUpdate()
    {
        // Corps cinématique : sa prochaine position, limitée à l'écran
        Vector2 step = _moveInput * (_moveSpeed * Time.fixedDeltaTime);
        Vector2 target = _rb.position + step;
        
        target.x = Mathf.Clamp(target.x, _minPosition.x, _maxPosition.x);
        target.y = Mathf.Clamp(target.y, _minPosition.y, _maxPosition.y);

        _rb.MovePosition(target);
    }

    /// <summary>
    /// Retire une vie au joueur, et le détruit à sa dernière vie.
    /// </summary>
    public void TakeDamage()
    {
        // Déjà détruit : un deuxième coup dans le même pas ne compte pas
        if (_lives <= 0)
        {
            return;
        }

        /* BLOC 13-4.4 À ACTIVER : effacez cette ligne et la ligne de fin du bloc
        // Le bouclier absorbe le coup à la place d'une vie
        if (_hasShield)
        {
            _hasShield = false;
            _shieldVisual.SetActive(false);
            return;
        }
        FIN DU BLOC 13-4.4 : effacez cette ligne */

        _lives--;
        LivesChanged?.Invoke(_lives);

        if (_lives > 0)
        {
            return;
        }

        Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        Died?.Invoke();
        Destroy(gameObject);
    }

    private void HandleFirePressed()
    {
        _isFireHeld = true;
    }

    private void HandleFireReleased()
    {
        _isFireHeld = false;
    }

    private void Fire()
    {
        Instantiate(_laserPrefab, _firePoint.position, Quaternion.identity);
        _nextFireTime = Time.time + _fireDelay;
    }

    /* BLOC 13-4.4 À ACTIVER : effacez cette ligne et la ligne de fin du bloc
    private IEnumerator RapidFireRoutine()
    {
        _currentFireDelay = _rapidFireDelay;
        yield return new WaitForSeconds(_powerUpDuration);
        _currentFireDelay = _fireDelay;
        _rapidFireRoutine = null;
    }
    FIN DU BLOC 13-4.4 : effacez cette ligne */

    private void ComputeLimits()
    {
        Camera mainCamera = Camera.main;

        Vector2 bottomLeft = mainCamera.ViewportToWorldPoint(Vector2.zero);
        Vector2 topRight = mainCamera.ViewportToWorldPoint(Vector2.one);

        Vector2 halfSize = _spriteRenderer.bounds.extents;
        _minPosition = bottomLeft + halfSize;
        _maxPosition = new Vector2(topRight.x - halfSize.x, _maxHeight);
    }
}
