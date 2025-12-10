using MediatR;
using Microsoft.AspNetCore.Mvc;
using WishesTracer.Application.DTOs;
using WishesTracer.Application.Features.Products.Commands.CreateProduct;
using WishesTracer.Shared.Extensions;

namespace WishesTracer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {

        private readonly IMediator _mediator;

        public ProductsController(IMediator mediator)
        {
            _mediator = mediator;
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
        
    }
}
