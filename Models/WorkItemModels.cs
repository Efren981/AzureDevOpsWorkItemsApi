using System.ComponentModel.DataAnnotations;

namespace AzureDevOpsWorkItemsApi.Models
{
    public class WorkItemCreateRequest
    {
        [Required]
        public string Title { get; set; }
        
        public string Description { get; set; }
        
        public string Type { get; set; } = "Task";  // Por defecto es una tarea
        
        public string AssignedTo { get; set; }
        
        public int? Priority { get; set; }
        
        public Dictionary<string, object> CustomFields { get; set; }
    }

    public class WorkItemResponse
    {
        public bool Success { get; set; }
        public int? WorkItemId { get; set; }
        public string Url { get; set; }
        public string Message { get; set; }
        public object Error { get; set; }
    }

    public class WorkItemUpdateRequest
    {
        [Required]
        public int Id { get; set; }
        
        public string Title { get; set; }
        
        public string Description { get; set; }
        
        public string State { get; set; }
        
        public string AssignedTo { get; set; }
        
        public int? Priority { get; set; }
        
        public Dictionary<string, object> CustomFields { get; set; }
    }
}