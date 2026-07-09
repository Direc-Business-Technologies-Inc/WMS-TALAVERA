using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Others.NS;

public class NetSuiteErrorResponse
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }


    [JsonPropertyName("o:errorDetails")]
    public List<ErrorDetail> Details { get; set; } = [];

    public class ErrorDetail
    {
        public string Detail { get; set; } = string.Empty;
        [JsonPropertyName("o:errorPath")]
        public string ErrorPath { get; set; } = string.Empty;
        [JsonPropertyName("o:errorCode")]
        public string ErrorCode { get; set; } = string.Empty;

        [JsonIgnore]
        public string DisplayString => $"[{ErrorCode}] {Detail}";
    }
    [JsonIgnore]
    public string DisplayString => $"{Title}({Status}):\n {string.Join("\n", Details.Select(x => x.DisplayString))}";
}
