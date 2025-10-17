using UnityEngine;
using System.Collections.Generic;

public class Settings : MonoBehaviour
{
    private List<ResolutionOption> resolutions = new List<ResolutionOption>
    {
        new ResolutionOption("800x600", 800, 600),
        new ResolutionOption("1024x768", 1024, 768),
        new ResolutionOption("1280x720", 1280, 720),
        new ResolutionOption("1920x1080", 1920, 1080)
    };

    private int currentIndex = 3; // Начинаем с максимального (1920x1080)
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