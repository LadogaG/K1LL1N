using System.Collections.Generic;
using UnityEngine;

public class Settings : MonoBehaviour
{
    private List<ResolutionOption> resolutions = new List<ResolutionOption>
    {
        new ResolutionOption("192x108", 192, 108),
        new ResolutionOption("384x216", 384, 216),
        new ResolutionOption("576x324", 576, 324),
        new ResolutionOption("800x600", 800, 600),
        new ResolutionOption("960x540", 960, 540),
        new ResolutionOption("1152x648", 1152, 648),
        new ResolutionOption("1280x720", 1280, 720),
        new ResolutionOption("1344x756", 1344, 756),
        new ResolutionOption("1536x864", 1536, 864),
        new ResolutionOption("1728x972", 1728, 972),
        new ResolutionOption("1920x1080", 1920, 1080)
    };

    private int currentIndex = 7; // Начинаем с максимального (1920x1080)
    private bool isFullScreen; // Флаг для отслеживания полноэкранного режима

    void Start()
    {
        // Проверяем текущий режим экрана
        isFullScreen = Screen.fullScreenMode == FullScreenMode.FullScreenWindow || Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen;
        Debug.Log($"Начальный режим: {(isFullScreen ? "Полноэкранный" : "Оконный")}");
        ApplyResolution(); // Применяем начальное разрешение
    }

    void Update()
    {
        // O — понижаем разрешение (меньше индекс)
        if (Input.GetKeyDown(KeyCode.O))
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                ApplyResolution();
                Debug.Log($"Разрешение понижено до {resolutions[currentIndex].Name}.");
            }
            else
            {
                Debug.Log("Минимальное разрешение достигнуто.");
            }
        }

        // P — повышаем разрешение (больше индекс)
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (currentIndex < resolutions.Count - 1)
            {
                currentIndex++;
                ApplyResolution();
                Debug.Log($"Разрешение повышено до {resolutions[currentIndex].Name}.");
            }
            else
            {
                Debug.Log("Максимальное разрешение достигнуто.");
            }
        }
    }

    void ApplyResolution()
    {
        ResolutionOption selected = resolutions[currentIndex];
        // Применяем разрешение с сохранением режима
        FullScreenMode mode = isFullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(selected.Width, selected.Height, mode);
        Debug.Log($"Применено разрешение: {selected.Name} (Ширина: {selected.Width}, Высота: {selected.Height}, Режим: {(isFullScreen ? "Полноэкранный" : "Оконный")})");
    }

    // Структура для хранения разрешений
    private class ResolutionOption
    {
        public string Name;
        public int Width;
        public int Height;

        public ResolutionOption(string name, int width, int height)
        {
            Name = name;
            Width = width;
            Height = height;
        }
    }
}