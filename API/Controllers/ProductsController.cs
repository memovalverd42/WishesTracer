// ProductsController.cs - API controller for product management operations
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Features.Products.Commands.CreateProduct;
using WishesTracer.Application.Features.Products.Queries;
using WishesTracer.Shared.DTOs;
using WishesTracer.Shared.Extensions;

namespace WishesTracer.Controllers
{
    /// <summary>
    /// API controller for managing product price tracking.
    /// </summary>
    /// <remarks>
    /// Provides endpoints for creating products to track, retrieving product information,
    /// viewing price history, and managing tracking status. All operations return RFC 7807
    /// Problem Details on error.
    /// </remarks>
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the ProductsController class.
        /// </summary>
        /// <param name="mediator">MediatR mediator for CQRS pattern</param>
        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Retrieves detailed information about a specific product.
        /// </summary>
        /// <param name="id">The unique identifier of the product</param>
        /// <returns>Product details including current price and price history</returns>
        /// <response code="200">Product details retrieved successfully</response>
        /// <response code="404">Product not found</response>
        /// <response code="400">Invalid product ID format</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProductDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
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
        /// <param name="searchTerm">
        /// Término de búsqueda para filtrar por nombre o URL del producto
        /// </param>
        /// <returns>Lista paginada de productos</returns>
        /// <response code="200">Products retrieved successfully</response>
        /// <response code="400">Invalid pagination parameters</response>
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

        /// <summary>
        /// Creates a new product for price tracking.
        /// </summary>
        /// <param name="command">The product creation command containing the URL</param>
        /// <returns>The created product with initial price information</returns>
        /// <response code="201">Product created successfully</response>
        /// <response code="400">Invalid URL or scraping failed</response>
        /// <response code="409">Product with this URL already exists</response>
        /// <remarks>
        /// The URL will be cleaned (query parameters removed) and validated. The system
        /// will scrape the product page to extract title, price, and availability info.
        /// Supported vendors: Amazon, MercadoLibre.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToValueOrProblemDetails();
        }

        /// <summary>
        /// Retrieves the price history for a specific product.
        /// </summary>
        /// <param name="id">The unique identifier of the product</param>
        /// <returns>List of historical price points ordered by timestamp</returns>
        /// <response code="200">Price history retrieved successfully</response>
        /// <response code="404">Product not found</response>
        /// <response code="400">Invalid product ID format</response>
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
