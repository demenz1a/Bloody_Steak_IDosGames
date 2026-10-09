using UnityEngine;

/// <summary>
/// Пара значений "в начале игры" / "на максимальной сложности". Промежуточное значение —
/// линейная интерполяция по текущей сложности (0..1).
/// </summary>
[System.Serializable]
public struct DifficultyRange
{
    public float easy;
    public float hard;

    public DifficultyRange(float easy, float hard)
    {
        this.easy = easy;
        this.hard = hard;
    }

    public float Evaluate(float difficulty01) => Mathf.Lerp(easy, hard, difficulty01);
}

/// <summary>
/// Единственный источник правды по сложности. Сложность — число 0..1, которое растёт
/// с игровым временем: 0 в начале смены, 1 через timeToMaxDifficulty секунд. Форму роста
/// задаёт difficultyCurve (по умолчанию плавный старт и плавный выход на максимум).
///
/// Сам менеджер ничего не меняет в других системах — он только отвечает на вопрос
/// "какое значение параметра сейчас". OrderManager и NpcSpawner спрашивают его в момент
/// создания заказа/клиента, поэтому уже созданные заказы и клиенты не меняются на лету.
///
/// Время считается по Time.deltaTime: при геймовере (timeScale = 0) рост останавливается.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    [Header("Progression")]
    [Tooltip("За сколько секунд игры сложность доходит до максимума.")]
    [SerializeField] private float timeToMaxDifficulty = 300f;
    [Tooltip("Форма роста: X — доля времени (0..1), Y — сложность (0..1).")]
    [SerializeField] private AnimationCurve difficultyCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("С какой сложности начинать — удобно для теста поздней игры.")]
    [Range(0f, 1f)]
    [SerializeField] private float startDifficulty = 0f;

    [Header("Orders")]
    [Tooltip("Сколько секунд клиент ждёт заказ.")]
    [SerializeField] private DifficultyRange orderDuration = new DifficultyRange(45f, 20f);
    [Tooltip("Шанс, что заказ будет из двух блюд.")]
    [SerializeField] private DifficultyRange twoItemOrderChance = new DifficultyRange(0.1f, 0.5f);

    [Header("Customers flow")]
    [Tooltip("Минимальный интервал между появлениями клиентов (сек).")]
    [SerializeField] private DifficultyRange minSpawnInterval = new DifficultyRange(8f, 3f);
    [Tooltip("Максимальный интервал между появлениями клиентов (сек).")]
    [SerializeField] private DifficultyRange maxSpawnInterval = new DifficultyRange(13f, 6f);

    [Header("Customer stay")]
    [Tooltip("Сколько секунд клиент ест за столиком.")]
    [SerializeField] private DifficultyRange eatDuration = new DifficultyRange(12f, 5f);
    [Tooltip("Сколько секунд клиент проводит в туалете.")]
    [SerializeField] private DifficultyRange toiletDuration = new DifficultyRange(6f, 3f);

    private float _elapsed;

    /// <summary>Текущая сложность 0..1.</summary>
    public float Difficulty { get; private set; }

    public float OrderDuration => orderDuration.Evaluate(Difficulty);
    public float TwoItemOrderChance => Mathf.Clamp01(twoItemOrderChance.Evaluate(Difficulty));
    public float EatDuration => eatDuration.Evaluate(Difficulty);
    public float ToiletDuration => toiletDuration.Evaluate(Difficulty);

    /// <summary>Случайный интервал до следующего клиента в текущих границах.</summary>
    public float NextSpawnInterval()
    {
        float min = minSpawnInterval.Evaluate(Difficulty);
        float max = Mathf.Max(min, maxSpawnInterval.Evaluate(Difficulty));
        return Random.Range(min, max);
    }

    private void Awake()
    {
        _elapsed = startDifficulty * timeToMaxDifficulty;
        UpdateDifficulty();
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        UpdateDifficulty();
    }

    private void UpdateDifficulty()
    {
        float time01 = timeToMaxDifficulty > 0f ? Mathf.Clamp01(_elapsed / timeToMaxDifficulty) : 1f;
        Difficulty = Mathf.Clamp01(difficultyCurve.Evaluate(time01));
    }
}
