namespace ZentavioCRM.Core.DTOs.Common
{
    public class ImportResultDto
    {
        public int TotalRows { get; set; }

        public int SuccessCount { get; set; }

        public int FailureCount { get; set; }

        public List<ImportRowErrorDto> Errors { get; set; } = [];
    }

    public class ImportRowErrorDto
    {
        /// <summary>1-based row number within the CSV, excluding the header row.</summary>
        public int RowNumber { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
