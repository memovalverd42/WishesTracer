using MediatR;
using Microsoft.AspNetCore.Mvc;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Features.Products.Commands.CreateProduct;
using WishesTracer.Application.Features.Products.Queries;
using WishesTracer.Shared.DTOs;
using WishesTracer.Shared.Extensions;

namespace WishesTracer.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ProductDetailsDto>> GetProduct(Guid id)
        {
            var result = await _mediator.Send(new GetProductDetailsQuery(id));
            return result.ToValueOrProblemDetails();
        }
        
        /// <summary>
        /// Obtiene una lista paginada de productos activos
        /// </summary>
        /// <param name="page">Número de página (inicia en 1)</param>
        /// <param name="pageSize">Cantidad de elementos por página (máximo 100)</param>
        /// <param name="searchTerm">Término de búsqueda para filtrar por nombre o URL del producto</param>
        /// <returns>Lista paginada de productos</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? searchTerm = null)
        {
            var result = await _mediator.Send(new GetProductsQuery(page, pageSize, searchTerm));
            return result.ToValueOrProblemDetails();
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToValueOrProblemDetails();
        }

        [HttpGet("{id:guid}/history")]
        [ProducesResponseType(typeof(List<PriceHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<PriceHistoryDto>>> GetHistory(Guid id)
        {
            var result = await _mediator.Send(new GetProductHistoryQuery(id));
            return result.ToValueOrProblemDetails();
        }

        // Listado de productos -> /api/products?page=1&pageSize=10&searchTerm=iphone

        // Eliminar Producto -> /api/products/{id}

        // Pausar/Reactivar Monitoreo
        // Ruta: /api/products/{id}/status
        // Body: { "isActive": false }

        // /api/stats/dashboard
    }
}
