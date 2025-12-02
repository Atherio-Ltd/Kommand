using Kommand.Abstractions;
using Kommand.Sample.Api.Commands.ProductCommands;
using Kommand.Sample.Api.DTOs;
using Kommand.Sample.Api.Models;
using Kommand.Sample.Api.Queries.ProductQueries;

namespace Kommand.Sample.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", ListProducts)
            .WithName("ListProducts")
            .WithSummary("List all products")
            .WithDescription("Returns a paginated list of products with optional filtering.")
            .Produces<object>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetProductById)
            .WithName("GetProductById")
            .WithSummary("Get product by ID")
            .WithDescription("Returns a single product by ID.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/by-sku/{sku}", GetProductBySku)
            .WithName("GetProductBySku")
            .WithSummary("Get product by SKU")
            .WithDescription("Returns a single product by SKU.")
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .WithSummary("Create a new product")
            .WithDescription("""
                Creates a new product. Demonstrates:
                - Command with multiple validation rules
                - Async validation (SKU uniqueness check)
                - Publishing notifications after success
                """)
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateProduct)
            .WithName("UpdateProduct")
            .WithSummary("Update an existing product")
            .WithDescription("Updates a product.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> ListProducts(
        IMediator mediator,
        decimal? minPrice,
        decimal? maxPrice,
        bool? inStockOnly,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new ListProductsQuery(minPrice, maxPrice, inStockOnly, page, pageSize);
        var result = await mediator.QueryAsync(query, ct);

        return Results.Ok(new
        {
            result.Items,
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage
        });
    }

    private static async Task<IResult> GetProductById(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetProductByIdQuery(id);
        var product = await mediator.QueryAsync(query, ct);

        if (product == null)
        {
            return Results.NotFound(new ErrorResponse(
                Title: "Not Found",
                Status: 404,
                Detail: $"Product with ID {id} not found"));
        }

        return Results.Ok(ToProductResponse(product));
    }

    private static async Task<IResult> GetProductBySku(
        string sku,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetProductBySkuQuery(sku);
        var product = await mediator.QueryAsync(query, ct);

        if (product == null)
        {
            return Results.NotFound(new ErrorResponse(
                Title: "Not Found",
                Status: 404,
                Detail: $"Product with SKU '{sku}' not found"));
        }

        return Results.Ok(ToProductResponse(product));
    }

    private static async Task<IResult> CreateProduct(
        CreateProductRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new CreateProductCommand(
            request.Name,
            request.Sku,
            request.Description,
            request.Price,
            request.StockQuantity);
        var product = await mediator.SendAsync(command, ct);

        return Results.Created($"/api/products/{product.Id}", ToProductResponse(product));
    }

    private static async Task<IResult> UpdateProduct(
        Guid id,
        UpdateProductRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new UpdateProductCommand(id, request.Name, request.Description, request.Price, request.StockQuantity);
        await mediator.SendAsync(command, ct);

        return Results.NoContent();
    }

    private static ProductResponse ToProductResponse(Product product) =>
        new(product.Id, product.Name, product.Sku, product.Description, product.Price,
            product.StockQuantity, product.CreatedAt, product.UpdatedAt, product.IsActive);
}
