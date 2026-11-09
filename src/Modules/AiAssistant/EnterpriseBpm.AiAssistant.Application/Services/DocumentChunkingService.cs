using System.Text.RegularExpressions;
using EnterpriseBpm.AiAssistant.Application.Contracts;
using EnterpriseBpm.AiAssistant.Application.DTOs;

namespace EnterpriseBpm.AiAssistant.Application.Services;

public class DocumentChunkingService : IDocumentChunkingService
{
    public IReadOnlyList<DocumentChunkDto> ChunkDocument(
        string text,
        string documentTitle,
        int chunkSize = 500,
        int overlap = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<DocumentChunkDto>();

        var chunks = new List<DocumentChunkDto>();
        // Ước tính 1 token ~ 4 ký tự tiếng Việt / từ
        int charChunkSize = chunkSize * 4;
        int charOverlap = overlap * 4;

        // Tách các đoạn văn bản (Paragraphs)
        var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var currentChunkText = new System.Text.StringBuilder();
        int chunkIndex = 0;
        int currentPage = 1;
        int chunkStartPage = 1;
        string currentSection = "Mở đầu";
        string chunkStartSection = "Mở đầu";

        var pageRegex = new Regex(@"(?:Trang|Page)\s+(\d+)", RegexOptions.IgnoreCase);
        var sectionRegex = new Regex(@"(?:Điều|Chương|Mục)\s+(\d+[^:\n]*)", RegexOptions.IgnoreCase);

        foreach (var p in paragraphs)
        {
            var trimmed = p.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (currentChunkText.Length == 0)
            {
                chunkStartPage = currentPage;
                chunkStartSection = currentSection;
            }

            // Kiểm tra số trang nếu có
            var pageMatch = pageRegex.Match(trimmed);
            int? detectedPage = null;
            if (pageMatch.Success && int.TryParse(pageMatch.Groups[1].Value, out var pg))
            {
                detectedPage = pg;
            }

            // Kiểm tra tiêu đề điều khoản nếu có
            var secMatch = sectionRegex.Match(trimmed);
            string? detectedSection = null;
            if (secMatch.Success)
            {
                detectedSection = secMatch.Value.Trim();
            }

            if (currentChunkText.Length + trimmed.Length > charChunkSize && currentChunkText.Length > 0)
            {
                chunks.Add(new DocumentChunkDto(
                    ChunkIndex: chunkIndex++,
                    Text: currentChunkText.ToString().Trim(),
                    PageNumber: chunkStartPage,
                    SectionTitle: chunkStartSection
                ));

                // Giữ lại phần overlap ở cuối chunk
                var fullText = currentChunkText.ToString();
                currentChunkText.Clear();
                if (fullText.Length > charOverlap)
                {
                    currentChunkText.Append(fullText.Substring(fullText.Length - charOverlap));
                    currentChunkText.Append("\n");
                }

                chunkStartPage = detectedPage ?? currentPage;
                chunkStartSection = detectedSection ?? currentSection;
            }

            if (detectedPage.HasValue)
            {
                currentPage = detectedPage.Value;
                if (currentChunkText.Length == 0) chunkStartPage = currentPage;
            }

            if (detectedSection != null)
            {
                currentSection = detectedSection;
                if (currentChunkText.Length == 0) chunkStartSection = currentSection;
            }

            currentChunkText.Append(trimmed);
            currentChunkText.Append("\n\n");
        }

        if (currentChunkText.Length > 0)
        {
            chunks.Add(new DocumentChunkDto(
                ChunkIndex: chunkIndex++,
                Text: currentChunkText.ToString().Trim(),
                PageNumber: chunkStartPage,
                SectionTitle: chunkStartSection
            ));
        }

        return chunks;
    }
}

