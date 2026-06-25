using System.Text.Json;

namespace Dokkana.BLL.AI.Embeddings;

public static class VectorHelper
{
    public static string ToJson(double[] vector)
    {
        return JsonSerializer.Serialize(vector);
    }

    public static double[] FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<double>();

        try
        {
            return JsonSerializer.Deserialize<double[]>(json)
                   ?? Array.Empty<double>();
        }
        catch
        {
            return Array.Empty<double>();
        }
    }

    public static double CosineSimilarity(
        double[] a,
        double[] b)
    {
        if (a.Length == 0 || b.Length == 0)
            return 0;

        var length = Math.Min(a.Length, b.Length);

        double dot = 0;
        double normA = 0;
        double normB = 0;

        for (var i = 0; i < length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
            return 0;

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}