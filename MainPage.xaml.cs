using Microsoft.Maui.Graphics.Text;
using Microsoft.Maui.Layouts;

namespace AppSorteoMobile;

public partial class MainPage : ContentPage
{
    Random rnd = new Random();

    bool modoEvento = false;
    List<long> historial = new List<long>();
    Dictionary<long, int> repeticiones = new Dictionary<long, int>();

    long? minRepeticiones = null;
    bool acumular = true;
    bool permitirRepetidos = true;

    public MainPage()
    {
        InitializeComponent();
    }

    // 🎰 Animación ruleta
    async Task AnimarRuleta(long total)
    {
        int delay = 30;

        for (int i = 0; i < 25; i++)
        {
            long num = rnd.Next(1, (int)total + 1);
            lblResultado.Text = $"🎰 Girando... {num}";
            await Task.Delay(delay);
            delay += 16;
        }
    }

    // 🎉 Confetti
    async Task LanzarConfetti()
    {
        confettiLayer.Children.Clear();

        Random r = new Random();

        for (int i = 0; i < 60; i++)
        {
            var emoji = new Label
            {
                Text = "🎉",
                FontSize = 22
            };

            double startX = r.NextDouble();

            AbsoluteLayout.SetLayoutBounds(emoji, new Rect(startX, 0, 30, 30));
            AbsoluteLayout.SetLayoutFlags(emoji, AbsoluteLayoutFlags.PositionProportional);

            confettiLayer.Children.Add(emoji);

            _ = Task.Run(async () =>
            {
                double y = 0;

                while (y < 1)
                {
                    y += 0.02;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        AbsoluteLayout.SetLayoutBounds(emoji, new Rect(startX, y, 30, 30));
                    });

                    await Task.Delay(10);
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    confettiLayer.Children.Remove(emoji);
                });
            });
        }
    }

    private void OnResetInputs(object sender, EventArgs e)
    {
        txtNumeros.Text = "";
        txtGanadores.Text = "";
        txtMinRepeticiones.Text = "";

        lblResultado.Text = "Sistema reiniciado ✔";
        lblResultado.TextColor = Colors.White;
    }

    private void OnResetHistorial(object sender, EventArgs e)
    {
        historial.Clear();
        lblHistorial.Text = "";
    }

    private void OnResetTop(object sender, EventArgs e)
    {
        repeticiones.Clear();
        lblResultado.Text = "🏆 Top de repetidos reiniciado";
    }

    // 🎯 BOTÓN PRINCIPAL
    private async void OnSortearClicked(object sender, EventArgs e)
    {
        if (!long.TryParse(txtNumeros.Text, out long total) ||
            !long.TryParse(txtGanadores.Text, out long ganadores))
        {
            lblResultado.Text = "Datos inválidos";
            return;
        }

        // Configuración
        minRepeticiones =
            long.TryParse(txtMinRepeticiones.Text, out long val)
            ? val
            : null;

        acumular = swAcumular.IsToggled;
        permitirRepetidos = swRepetidos.IsToggled;
        modoEvento = swEvento.IsToggled;

        await AnimarRuleta(total);

        HashSet<long> resultados = new HashSet<long>();

        while (resultados.Count < ganadores)
        {
            long numero = rnd.Next(1, (int)total + 1);

            if (!permitirRepetidos && historial.Contains(numero))
                continue;

            resultados.Add(numero);
            historial.Add(numero);

            if (repeticiones.ContainsKey(numero))
                repeticiones[numero]++;
            else
                repeticiones[numero] = 1;
        }

        // Historial
        lblHistorial.Text = string.Join(" | ", historial.TakeLast(50));

        // Top repetidos
        var top = repeticiones
            .OrderByDescending(x => x.Value)
            .Take(5)
            .Select(x => $"#{x.Key} → {x.Value} veces");

        // Destacados
        string destacadosTexto = "";

        if (minRepeticiones != null)
        {
            var ganadoresDestacados = resultados.Where(n =>
                repeticiones.ContainsKey(n) &&
                repeticiones[n] >= minRepeticiones
            ).ToList();

            destacadosTexto =
                $"🔥 DESTACADOS (≥ {minRepeticiones}):\n" +
                string.Join(", ", ganadoresDestacados) + "\n\n";
        }

        // 🎨 FORMATO VISUAL
        var formatted = new FormattedString();

        // Título
        formatted.Spans.Add(new Span
        {
            Text = "🎉 SORTEADOS:\n",
            FontSize = 24,
            TextColor = Colors.White
        });

        // 🔥 SOLO NÚMEROS GRANDES
        foreach (var num in resultados)
        {
            formatted.Spans.Add(new Span
            {
                Text = num + " | ",
                FontSize = 32,
                FontAttributes = FontAttributes.Bold,
                TextColor = modoEvento
                    ? Colors.Gold
                    : Color.FromArgb("#00FFAA")
            });
        }

        // Espacio
        formatted.Spans.Add(new Span { Text = "\n\n" });

        // Texto normal (NO gigante)
        formatted.Spans.Add(new Span
        {
            Text =
                destacadosTexto +
                "🏆 TOP REPETIDOS:\n" +
                string.Join("\n", top),
            FontSize = 20,
            TextColor = Colors.LightGray
        });

        // Aplicar
        lblResultado.FormattedText = formatted;

        // Confetti
        if (modoEvento)
        {
            await LanzarConfetti();
        }
    }
}