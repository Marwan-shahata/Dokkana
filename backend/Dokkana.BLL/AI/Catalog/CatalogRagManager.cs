using System.Text.Json;
using Dokkana.BLL.AI.Embeddings;
using Dokkana.BLL.AI.OpenRouter;
using Dokkana.BLL.DTOs.AI.Catalog;
using Dokkana.DAL.Data;
using Dokkana.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.AI.Catalog;

public class CatalogRagManager : ICatalogRagManager
{
    private readonly AppDbContext _context;
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly ITextChunkingService _textChunkingService;
    private readonly IOpenRouterClient _openRouterClient;
    private readonly IEmbeddingService _embeddingService;
    private readonly ICatalogRetrievalService _catalogRetrievalService;

    public CatalogRagManager(
     AppDbContext context,
     IPdfTextExtractor pdfTextExtractor,
     ITextChunkingService textChunkingService,
     IOpenRouterClient openRouterClient,
     IEmbeddingService embeddingService,
     ICatalogRetrievalService catalogRetrievalService)
    {
        _context = context;
        _pdfTextExtractor = pdfTextExtractor;
        _textChunkingService = textChunkingService;
        _openRouterClient = openRouterClient;
        _embeddingService = embeddingService;
        _catalogRetrievalService = catalogRetrievalService;
    }

    public async Task<CatalogUploadResponseDto> UploadCatalogAsync(
        int storeId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (storeId <= 0)
        {
            return new CatalogUploadResponseDto
            {
                StoreId = storeId,
                Status = "Failed",
                Message = "StoreId غير صحيح."
            };
        }

        if (fileStream == null || fileStream.Length == 0)
        {
            return new CatalogUploadResponseDto
            {
                StoreId = storeId,
                FileName = fileName,
                Status = "Failed",
                Message = "الملف فارغ."
            };
        }

        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return new CatalogUploadResponseDto
            {
                StoreId = storeId,
                FileName = fileName,
                Status = "Failed",
                Message = "ارفع ملف PDF فقط."
            };
        }

        var document = new CatalogDocument
        {
            StoreId = storeId,
            FileName = fileName,
            Status = CatalogStatus.Processing,
            UploadedAt = DateTime.UtcNow
        };

        _context.CatalogDocuments.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var extractedText = await _pdfTextExtractor.ExtractTextAsync(
                fileStream,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                document.Status = CatalogStatus.Failed;
                await _context.SaveChangesAsync(cancellationToken);

                return new CatalogUploadResponseDto
                {
                    CatalogDocumentId = document.Id,
                    StoreId = storeId,
                    FileName = fileName,
                    Status = "Failed",
                    Message = "لم يتم استخراج نص من ملف PDF. تأكد أن الملف ليس صورة فقط."
                };
            }

            var chunks = _textChunkingService.SplitIntoChunks(extractedText);

            if (chunks.Count == 0)
            {
                document.Status = CatalogStatus.Failed;
                await _context.SaveChangesAsync(cancellationToken);

                return new CatalogUploadResponseDto
                {
                    CatalogDocumentId = document.Id,
                    StoreId = storeId,
                    FileName = fileName,
                    Status = "Failed",
                    Message = "لم يتم إنشاء chunks من الملف."
                };
            }

            IReadOnlyList<double[]> embeddings = new List<double[]>();
            var vectorEnabled = true;

            try
            {
                embeddings = await _embeddingService.EmbedManyAsync(
                    chunks,
                    cancellationToken);
            }
            catch
            {
                vectorEnabled = false;
            }

            for (var i = 0; i < chunks.Count; i++)
            {
                var vector = embeddings.Count > i
                    ? embeddings[i]
                    : Array.Empty<double>();

                _context.CatalogChunks.Add(new DocumentChunk
                {
                    CatalogDocumentId = document.Id,
                    StoreId = storeId,
                    ChunkIndex = i,
                    PageNumber = null,
                    Content = chunks[i],
                    EmbeddingJson = vector.Length > 0
                        ? VectorHelper.ToJson(vector)
                        : null,
                    EmbeddingDimensions = vector.Length > 0
                        ? vector.Length
                        : null
                });
            }

            document.Status = CatalogStatus.Ready;

            await _context.SaveChangesAsync(cancellationToken);

            return new CatalogUploadResponseDto
            {
                CatalogDocumentId = document.Id,
                StoreId = storeId,
                FileName = fileName,
                Status = document.Status.ToString(),
                ChunksCount = chunks.Count,
                Message = vectorEnabled
                    ? "تم رفع الكتالوج واستخراج النص وإنشاء embeddings بنجاح."
                    : "تم رفع الكتالوج واستخراج النص، لكن embeddings فشلت وسيتم استخدام keyword search."
            };
        }
        catch
        {
            document.Status = CatalogStatus.Failed;
            await _context.SaveChangesAsync(cancellationToken);

            return new CatalogUploadResponseDto
            {
                CatalogDocumentId = document.Id,
                StoreId = storeId,
                FileName = fileName,
                Status = "Failed",
                Message = "حصل خطأ أثناء قراءة ملف PDF."
            };
        }
    }

    public async Task<CatalogQuestionResponseDto> AskAsync(
    CatalogQuestionRequestDto request,
    CancellationToken cancellationToken = default)
    {
        if (request.StoreId <= 0)
        {
            return new CatalogQuestionResponseDto
            {
                StoreId = request.StoreId,
                Question = request.Question,
                Answer = "StoreId غير صحيح."
            };
        }

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return new CatalogQuestionResponseDto
            {
                StoreId = request.StoreId,
                Question = request.Question,
                Answer = "اكتب سؤال عن الكتالوج."
            };
        }

        var retrievedChunks = await _catalogRetrievalService.RetrieveAsync(
            request.StoreId,
            request.Question,
            request.MaxChunks,
            cancellationToken);

        if (retrievedChunks.Count == 0)
        {
            return new CatalogQuestionResponseDto
            {
                StoreId = request.StoreId,
                Question = request.Question,
                RetrievalMode = "fallback",
                Answer = "مش لاقي معلومات مناسبة في الكتالوجات المرفوعة للمحل ده."
            };
        }

        var chunkContents = retrievedChunks
            .Select(x => x.Content)
            .ToList();

        var answer = await GenerateAnswerAsync(
            request.Question,
            chunkContents,
            cancellationToken);

        var hasVectorScores = retrievedChunks.Any(x => x.VectorScore > 0);
        var hasKeywordScores = retrievedChunks.Any(x => x.KeywordScore > 0);

        var retrievalMode =
            hasVectorScores && hasKeywordScores ? "hybrid" :
            hasVectorScores ? "vector" :
            hasKeywordScores ? "keyword" :
            "fallback";

        return new CatalogQuestionResponseDto
        {
            StoreId = request.StoreId,
            Question = request.Question,
            RetrievalMode = retrievalMode,
            Answer = answer,
            UsedChunks = chunkContents,
            RetrievedChunks = retrievedChunks
        };
    }


    private async Task<string> GenerateAnswerAsync(
        string question,
        List<string> chunks,
        CancellationToken cancellationToken)
    {
        var systemPrompt = """
You are a catalog Q&A assistant for a local fashion store.

Answer using only the provided catalog chunks.
Do not use outside knowledge.
Do not invent products, prices, colors, sizes, stock, or policies.
If the chunks do not contain the answer, say clearly that the catalog does not contain enough information.
Answer in the same language as the user's question.
If the user writes Arabic or Franco-Arabic, answer in simple Egyptian Arabic.
Keep the answer concise and useful.
""";

        var userPrompt = $"""
Question:
{question}

Catalog chunks:
{JsonSerializer.Serialize(chunks)}
""";

        try
        {
            var answer = await _openRouterClient.AskAsync(
                systemPrompt,
                userPrompt,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(answer))
                return "مش قادر أطلع إجابة واضحة من الكتالوج.";

            return answer.Trim();
        }
        catch
        {
            return "حصل خطأ أثناء توليد الإجابة من الكتالوج.";
        }
    }
}