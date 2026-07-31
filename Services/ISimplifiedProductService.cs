using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.AI.McpApp.Models;

namespace Nop.Plugin.AI.McpApp.Services;

public interface ISimplifiedProductService
{
    Task<IPagedList<Product>> SearchProductsAsync(SearchCatalogRequest request);
}