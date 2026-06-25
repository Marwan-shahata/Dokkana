using Dokkana.BLL.AI.Search;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dokkana.BLL.AI.BackgroundServices;

// Keeps the product semantic index up to date automatically, so new/changed
// products become searchable by meaning without anyone calling /reindex by hand.
//
// Adapted from the "Dokkana Ai Test" background-worker idea, but built on our
// SQL-Server embedding store (ProductEmbeddings) via IProductSemanticSearchService
// — no external vector database. With no OpenRouter key it stays idle.
public class ProductEmbeddingBackgroundService : BackgroundService
{
    // How often to look for products that still need embedding. The scan itself is
    // cheap (one DB read) and only calls the embedding API when something is new.
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductEmbeddingBackgroundService> _logger;

    public ProductEmbeddingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductEmbeddingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let startup migration + seeding finish first.
        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var semantic = scope.ServiceProvider.GetRequiredService<IProductSemanticSearchService>();

                if (semantic.IsConfigured)
                {
                    // forceRebuild:false only embeds products that are missing an
                    // embedding or whose text/model changed — so this is a no-op
                    // once everything is indexed.
                    var result = await semantic.ReindexAsync(forceRebuild: false, stoppingToken);
                    if (result.UpdatedProducts > 0 || result.FailedProducts > 0)
                    {
                        _logger.LogInformation(
                            "Product embeddings updated: {Updated} new, {Failed} failed.",
                            result.UpdatedProducts, result.FailedProducts);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Product embedding background scan failed.");
            }

            try { await Task.Delay(ScanInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
