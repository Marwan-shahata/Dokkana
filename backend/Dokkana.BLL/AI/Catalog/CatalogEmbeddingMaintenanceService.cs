using Dokkana.BLL.AI.Embeddings;
using Dokkana.BLL.AI.OpenRouter;
using Dokkana.BLL.DTOs.AI.Catalog;
using Dokkana.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dokkana.BLL.AI.Catalog;

public class CatalogEmbeddingMaintenanceService : ICatalogEmbeddingMaintenanceService
{
    private readonly AppDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly OpenRouterOptions _options;

    public CatalogEmbeddingMaintenanceService(
        AppDbContext context,
        IEmbeddingService embeddingService,
        IOptions<OpenRouterOptions> options)
    {
        _context = context;
        _embeddingService = embeddingService;
        _options = options.Value;
    }

    public async Task<RebuildCatalogEmbeddingsResponseDto> RebuildAsync(
        RebuildCatalogEmbeddingsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var batchSize = NormalizeBatchSize(request.BatchSize);

        var baseQuery = _context.CatalogChunks
            .Where(c =>
                c.CatalogDocument != null &&
                c.CatalogDocument.Status.ToString() == "Ready");

        if (request.StoreId.HasValue)
        {
            baseQuery = baseQuery.Where(c => c.StoreId == request.StoreId.Value);
        }

        if (request.CatalogDocumentId.HasValue)
        {
            baseQuery = baseQuery.Where(c => c.CatalogDocumentId == request.CatalogDocumentId.Value);
        }

        if (!request.ForceRebuild)
        {
            baseQuery = baseQuery.Where(c =>
                c.EmbeddingJson == null ||
                c.EmbeddingJson == "" ||
                c.EmbeddingModel != _options.EmbeddingModel);
        }

        var response = new RebuildCatalogEmbeddingsResponseDto
        {
            EmbeddingModel = _options.EmbeddingModel
        };

        var lastId = 0;

        while (true)
        {
            var batch = await baseQuery
                .AsNoTracking()
                .Where(c => c.Id > lastId)
                .OrderBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Content
                })
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
                break;

            lastId = batch.Max(x => x.Id);

            response.ProcessedChunks += batch.Count;

            IReadOnlyList<double[]> embeddings;

            try
            {
                embeddings = await _embeddingService.EmbedManyAsync(
                    batch.Select(x => x.Content).ToList(),
                    cancellationToken);
            }
            catch
            {
                response.FailedChunks += batch.Count;
                continue;
            }

            var ids = batch.Select(x => x.Id).ToList();

            var chunksToUpdate = await _context.CatalogChunks
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            foreach (var chunk in chunksToUpdate)
            {
                var index = batch.FindIndex(x => x.Id == chunk.Id);

                if (index < 0 || index >= embeddings.Count)
                {
                    response.FailedChunks++;
                    continue;
                }

                var vector = embeddings[index];

                if (vector.Length == 0)
                {
                    response.FailedChunks++;
                    continue;
                }

                chunk.EmbeddingJson = VectorHelper.ToJson(vector);
                chunk.EmbeddingDimensions = vector.Length;
                chunk.EmbeddingModel = _options.EmbeddingModel;

                response.UpdatedChunks++;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        response.Message = BuildMessage(response);

        return response;
    }

    private static int NormalizeBatchSize(int batchSize)
    {
        if (batchSize <= 0)
            return 16;

        if (batchSize > 64)
            return 64;

        return batchSize;
    }

    private static string BuildMessage(
        RebuildCatalogEmbeddingsResponseDto response)
    {
        if (response.ProcessedChunks == 0)
            return "لا توجد chunks تحتاج إلى إعادة توليد embeddings.";

        return $"تمت معالجة {response.ProcessedChunks} chunks. تم تحديث {response.UpdatedChunks} و فشل {response.FailedChunks}.";
    }
}