using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Vendors;
using Nop.Data;
using Nop.Plugin.AI.McpApp.Models;
using Nop.Services.Catalog;
using Nop.Services.Security;
using Nop.Services.Stores;

namespace Nop.Plugin.AI.McpApp.Services;

public class SimplifiedProductService : ISimplifiedProductService
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<Manufacturer> _manufacturerRepository;
    private readonly IRepository<Vendor> _vendorRepository;
    private readonly IRepository<ProductTag> _productTagRepository;
    private readonly IRepository<ProductCategory> _productCategoryRepository;
    private readonly IRepository<ProductManufacturer> _productManufacturerRepository;
    private readonly IStoreMappingService _storeMappingService;
    private readonly IAclService _aclService;
    private readonly IStoreContext _storeContext;
    private readonly IWorkContext _workContext;
    private readonly IRepository<ProductProductTagMapping> _productTagMappingRepository;

    public SimplifiedProductService(IRepository<Product> productRepository,
        IRepository<Category> categoryRepository,
        IRepository<Manufacturer> manufacturerRepository,
        IRepository<ProductTag> productTagRepository,
        IRepository<Vendor> vendorRepository,
        IRepository<ProductManufacturer> productManufacturerRepository,
        IWorkContext workContext,
        IStoreContext storeContext,
        IAclService aclService,
        IStoreMappingService storeMappingService,
        IRepository<ProductCategory> productCategoryRepository,
        IRepository<ProductProductTagMapping> productTagMappingRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _manufacturerRepository = manufacturerRepository;
        _productTagRepository = productTagRepository;
        _vendorRepository = vendorRepository;
        _productManufacturerRepository = productManufacturerRepository;
        _storeMappingService = storeMappingService;
        _aclService = aclService;
        _storeContext = storeContext;
        _workContext = workContext;
        _productCategoryRepository = productCategoryRepository;
        _productTagMappingRepository = productTagMappingRepository;
    }

    public virtual async Task<IPagedList<Product>> SearchProductsAsync(SearchCatalogRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var pageIndex = request.PageIndex < 0 ? 0 : request.PageIndex;
        var pageSize = request.PageSize <= 0 ? 15 : request.PageSize;

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        var productsQuery = _productRepository.Table
            .Where(p => !p.Deleted && p.Published);

        // store mapping + ACL, same as core search
        productsQuery = await _storeMappingService.ApplyStoreMapping(productsQuery, store.Id);
        productsQuery = await _aclService.ApplyAcl(productsQuery, customer);

        // free-text query: name / description / SKU
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var q = request.Query.Trim().ToLower();

            productsQuery = productsQuery.Where(p =>
                p.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
                p.ShortDescription.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
                p.FullDescription.Contains(q, StringComparison.CurrentCultureIgnoreCase) ||
                p.Sku.Contains(q, StringComparison.CurrentCultureIgnoreCase));
        }

        // categories resolved by (fuzzy, case-insensitive) name -> id
        if (request.Categories?.Length > 0)
        {
            var categoryIds = await ResolveIdsAsync(
                request.Categories,
                name => _categoryRepository.Table.Where(c => !c.Deleted && c.Name.Contains(name, StringComparison.CurrentCultureIgnoreCase)).Select(c => c.Id));

            if (categoryIds.Count > 0)
            {
                productsQuery =
                    from p in productsQuery
                    join pc in _productCategoryRepository.Table on p.Id equals pc.ProductId
                    where categoryIds.Contains(pc.CategoryId)
                    select p;
            }
        }

        // manufacturers resolved by name -> id
        if (request.Manufacturers?.Length > 0)
        {
            var manufacturerIds = await ResolveIdsAsync(
                request.Manufacturers,
                name => _manufacturerRepository.Table.Where(m => !m.Deleted && m.Name.Contains(name, StringComparison.CurrentCultureIgnoreCase)).Select(m => m.Id));

            if (manufacturerIds.Count > 0)
            {
                productsQuery =
                    from p in productsQuery
                    join pm in _productManufacturerRepository.Table on p.Id equals pm.ProductId
                    where manufacturerIds.Contains(pm.ManufacturerId)
                    select p;
            }
        }

        // vendor: single value, best-effort fuzzy match
        if (!string.IsNullOrWhiteSpace(request.Vendor))
        {
            var vendorNeedle = request.Vendor.Trim().ToLower();

            var vendorId = await _vendorRepository.Table
                .Where(v => !v.Deleted && v.Active && v.Name.Contains(vendorNeedle, StringComparison.CurrentCultureIgnoreCase))
                .Select(v => v.Id)
                .FirstOrDefaultAsync();

            if (vendorId > 0)
                productsQuery = productsQuery.Where(p => p.VendorId == vendorId);
        }

        // tags resolved by name -> id
        if (request.Tags?.Length > 0)
        {
            var tagIds = await ResolveIdsAsync(
                request.Tags,
                name => _productTagRepository.Table.Where(t => t.Name.Contains(name, StringComparison.CurrentCultureIgnoreCase)).Select(t => t.Id));

            if (tagIds.Count > 0)
            {
                productsQuery =
                    from p in productsQuery
                    join ptm in _productTagMappingRepository.Table on p.Id equals ptm.ProductId
                    where tagIds.Contains(ptm.ProductTagId)
                    select p;
            }
        }

        // price range
        var priceMin = request.PriceMin ?? decimal.Zero;
        var priceMax = request.PriceMax ?? decimal.MaxValue;
        productsQuery = productsQuery.Where(p => p.Price >= priceMin && p.Price <= priceMax);

        // joins above can duplicate rows (multiple category/tag matches per product)
        productsQuery = productsQuery.Distinct().OrderBy(p => p.Name);

        return await productsQuery.ToPagedListAsync(pageIndex, pageSize);
    }

    private static async Task<List<int>> ResolveIdsAsync(
        string[] names,
        Func<string, IQueryable<int>> queryFactory)
    {
        IQueryable<int> matches = null;

        foreach (var name in names.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            var idsForName = queryFactory(name.Trim());
            matches = matches == null ? idsForName : matches.Union(idsForName);
        }

        return matches == null
            ? new List<int>()
            : await matches.Distinct().ToListAsync();
    }
}
