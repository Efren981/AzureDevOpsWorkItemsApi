using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AzureDevOpsWorkItemsApi.Models;
using AzureDevOpsWorkItemsApi.Services;

namespace AzureDevOpsWorkItemsApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requiere autenticación JWT
    public class WorkItemsController : ControllerBase
    {
        private readonly IAzureDevOpsService _azureDevOpsService;
        private readonly ILogger<WorkItemsController> _logger;

        public WorkItemsController(
            IAzureDevOpsService azureDevOpsService,
            ILogger<WorkItemsController> logger)
        {
            _azureDevOpsService = azureDevOpsService;
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo Work Item en Azure DevOps
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateWorkItem([FromBody] WorkItemCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new WorkItemResponse
                {
                    Success = false,
                    Message = "Datos inválidos",
                    Error = ModelState
                });
            }

            _logger.LogInformation("Creando nuevo Work Item: {Title}", request.Title);
            
            var response = await _azureDevOpsService.CreateWorkItemAsync(request);
            
            if (response.Success)
            {
                _logger.LogInformation("Work Item creado exitosamente. ID: {WorkItemId}", response.WorkItemId);
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("Error al crear Work Item: {Error}", response.Error);
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Actualiza un Work Item existente en Azure DevOps
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(WorkItemResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateWorkItem(int id, [FromBody] WorkItemUpdateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new WorkItemResponse
                {
                    Success = false,
                    Message = "Datos inválidos",
                    Error = ModelState
                });
            }

            // Asegurar que el ID en la ruta coincida con el del cuerpo
            if (id != request.Id)
            {
                return BadRequest(new WorkItemResponse
                {
                    Success = false,
                    Message = "El ID en la ruta no coincide con el ID en el cuerpo"
                });
            }

            _logger.LogInformation("Actualizando Work Item: {Id}", id);
            
            var response = await _azureDevOpsService.UpdateWorkItemAsync(request);
            
            if (response.Success)
            {
                _logger.LogInformation("Work Item actualizado exitosamente. ID: {WorkItemId}", response.WorkItemId);
                return Ok(response);
            }
            else
            {
                _logger.LogWarning("Error al actualizar Work Item: {Error}", response.Error);
                
                // Si no se encuentra el Work Item
                if (response.Message.Contains("404"))
                {
                    return NotFound(new WorkItemResponse
                    {
                        Success = false,
                        Message = $"No se encontró el Work Item con ID {id}"
                    });
                }
                
                return StatusCode(500, response);
            }
        }
    }
}