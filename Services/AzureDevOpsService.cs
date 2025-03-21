using System.Net.Http.Headers;
using System.Text;
using AzureDevOpsWorkItemsApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureDevOpsWorkItemsApi.Services
{
    public interface IAzureDevOpsService
    {
        Task<WorkItemResponse> CreateWorkItemAsync(WorkItemCreateRequest request);
        Task<WorkItemResponse> UpdateWorkItemAsync(WorkItemUpdateRequest request);
    }

    public class AzureDevOpsService : IAzureDevOpsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _organization;
        private readonly string _project;
        private readonly string _pat;

        public AzureDevOpsService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            
            // Obtener configuración de Azure DevOps
            var adoConfig = _configuration.GetSection("AzureDevOps");
            _organization = adoConfig["Organization"];
            _project = adoConfig["Project"];
            _pat = adoConfig["Pat"];
            
            if (string.IsNullOrEmpty(_organization) || string.IsNullOrEmpty(_project) || string.IsNullOrEmpty(_pat))
            {
                throw new ArgumentException("La configuración de Azure DevOps es incompleta. Verifica appsettings.json");
            }
        }

        public async Task<WorkItemResponse> CreateWorkItemAsync(WorkItemCreateRequest request)
        {
            try
            {
                var client = CreateHttpClient();
                var workItemType = string.IsNullOrEmpty(request.Type) ? "Task" : request.Type;
                var url = $"https://dev.azure.com/{_organization}/{_project}/_apis/wit/workitems/${workItemType}?api-version=7.0";
                
                var operations = new List<object>();
                
                // Título (obligatorio)
                operations.Add(new
                {
                    op = "add",
                    path = "/fields/System.Title",
                    value = request.Title
                });
                
                // Descripción
                if (!string.IsNullOrEmpty(request.Description))
                {
                    operations.Add(new
                    {
                        op = "add",
                        path = "/fields/System.Description",
                        value = request.Description
                    });
                }
                
                // Asignado a
                if (!string.IsNullOrEmpty(request.AssignedTo))
                {
                    operations.Add(new
                    {
                        op = "add",
                        path = "/fields/System.AssignedTo",
                        value = request.AssignedTo
                    });
                }
                
                // Prioridad
                if (request.Priority.HasValue)
                {
                    operations.Add(new
                    {
                        op = "add",
                        path = "/fields/Microsoft.VSTS.Common.Priority",
                        value = request.Priority.Value
                    });
                }
                
                // Campos personalizados
                if (request.CustomFields != null)
                {
                    foreach (var field in request.CustomFields)
                    {
                        operations.Add(new
                        {
                            op = "add",
                            path = $"/fields/{field.Key}",
                            value = field.Value
                        });
                    }
                }
                
                var content = new StringContent(
                    JsonConvert.SerializeObject(operations),
                    Encoding.UTF8,
                    "application/json-patch+json");
                
                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var workItem = JObject.Parse(responseContent);
                    return new WorkItemResponse
                    {
                        Success = true,
                        WorkItemId = workItem["id"]?.Value<int>(),
                        Url = workItem["_links"]?["html"]?["href"]?.Value<string>(),
                        Message = "Work Item creado exitosamente"
                    };
                }
                else
                {
                    return new WorkItemResponse
                    {
                        Success = false,
                        Message = $"Error al crear Work Item: {response.StatusCode}",
                        Error = responseContent
                    };
                }
            }
            catch (Exception ex)
            {
                return new WorkItemResponse
                {
                    Success = false,
                    Message = "Error interno al crear Work Item",
                    Error = ex.Message
                };
            }
        }

        public async Task<WorkItemResponse> UpdateWorkItemAsync(WorkItemUpdateRequest request)
        {
            try
            {
                var client = CreateHttpClient();
                var url = $"https://dev.azure.com/{_organization}/{_project}/_apis/wit/workitems/{request.Id}?api-version=7.0";
                
                var operations = new List<object>();
                
                // Título
                if (!string.IsNullOrEmpty(request.Title))
                {
                    operations.Add(new
                    {
                        op = "add",
                        path = "/fields/System.Title",
                        value = request.Title
                    });
                }
                
                // Descripción
                if (!string.IsNullOrEmpty(request.Description))
                {
                    operations.Add(new
                    {
                        op = "add",
                        path = "/fields/System.Description",
                        value = request.Description
                    });
                }
                
                // Estado
                if (!string.IsNullOrEmpty(request.State))
                {
                    operations.Add(new
                    {
                        op = "add",
                        path = "/fields/System.State",
                        value = request.State
                    });
                }
                
                // Asignado a
                if (!string.IsNullOrEmpty(request.AssignedTo))
                {
                    operations.Add(new
                    {
                        op = "add",
                        path = "/fields/System.AssignedTo",
                        value = request.AssignedTo
                    });
                }
                
                // Prioridad
                if (request.Priority.HasValue)
                {
                    operations.Add(new
                    {
                        op = "add",
                        path = "/fields/Microsoft.VSTS.Common.Priority",
                        value = request.Priority.Value
                    });
                }
                
                // Campos personalizados
                if (request.CustomFields != null)
                {
                    foreach (var field in request.CustomFields)
                    {
                        operations.Add(new
                        {
                            op = "add",
                            path = $"/fields/{field.Key}",
                            value = field.Value
                        });
                    }
                }
                
                // Si no hay operaciones, devolver error
                if (operations.Count == 0)
                {
                    return new WorkItemResponse
                    {
                        Success = false,
                        Message = "No se especificaron campos para actualizar"
                    };
                }
                
                var content = new StringContent(
                    JsonConvert.SerializeObject(operations),
                    Encoding.UTF8,
                    "application/json-patch+json");
                
                var response = await client.PatchAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var workItem = JObject.Parse(responseContent);
                    return new WorkItemResponse
                    {
                        Success = true,
                        WorkItemId = workItem["id"]?.Value<int>(),
                        Url = workItem["_links"]?["html"]?["href"]?.Value<string>(),
                        Message = "Work Item actualizado exitosamente"
                    };
                }
                else
                {
                    return new WorkItemResponse
                    {
                        Success = false,
                        Message = $"Error al actualizar Work Item: {response.StatusCode}",
                        Error = responseContent
                    };
                }
            }
            catch (Exception ex)
            {
                return new WorkItemResponse
                {
                    Success = false,
                    Message = "Error interno al actualizar Work Item",
                    Error = ex.Message
                };
            }
        }

        private HttpClient CreateHttpClient()
        {
            var client = _httpClientFactory.CreateClient();
            
            // Configurar autenticación con PAT
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_pat}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            
            return client;
        }
    }
}