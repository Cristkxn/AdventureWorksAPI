using AdventureWorksNS.Data;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Concurrent;

namespace AdventureWorksAPI.Repositories
{
    public class ProductCategoryRepository : IProductCategoryRepository
    {
        private static ConcurrentDictionary<int, ProductCategory>? ProductCategoryCache;
        //Comentario, puede usar Redis para un cache mas eficiente ==> Open Source
        private AdventureWorksDB db;

        public ProductCategoryRepository(AdventureWorksDB injectedDB)
        {
            db = injectedDB;
            if (ProductCategoryCache is null)
            {
                ProductCategoryCache = new ConcurrentDictionary<int, ProductCategory>(
                    db.ProductCategories.ToDictionary(p => p.ProductCategoryId));
            }
        }

        public async Task<ProductCategory> CreateAsync(ProductCategory p)
        {
            EntityEntry<ProductCategory> agregado = await db.ProductCategories.AddAsync(p);
            int afectados = await db.SaveChangesAsync();
            if (afectados == 1)
            {
                if (ProductCategoryCache is null) return p;
                return ProductCategoryCache.AddOrUpdate(p.ProductCategoryId, p, UpdateCache);
            }
            return null!;
        }

        private ProductCategory UpdateCache(int id, ProductCategory p)
        {
            ProductCategory? viejo;
            if (ProductCategoryCache is not null)
            {
                if (ProductCategoryCache.TryGetValue(id, out viejo))
                {
                    if (ProductCategoryCache.TryUpdate(id, p, viejo))
                    {
                        return p;
                    }
                }
            }
            return null!;
        }

        public Task<IEnumerable<ProductCategory>> RetrieveAllAsync()
        {
            return Task.FromResult(ProductCategoryCache is null ?
                Enumerable.Empty<ProductCategory>() : ProductCategoryCache.Values);
        }

        public Task<ProductCategory?> RetrieveAsync(int id)
        {
            if (ProductCategoryCache is null) return null!;
            ProductCategoryCache.TryGetValue(id, out ProductCategory? p);
            return Task.FromResult(p);
        }

        public async Task<ProductCategory?> UpdateAsync(int id, ProductCategory p)
        {
            db.ProductCategories.Update(p);
            int afectados = await db.SaveChangesAsync();
            if (afectados == 1)
            {
                return UpdateCache(id, p);
            }
            return null;
        }

        public async Task<bool?> DeleteAsync(int id)
        {
            ProductCategory? p = db.ProductCategories.Find(id);
            if (p is null) return false;
            db.ProductCategories.Remove(p);
            int afectados = await db.SaveChangesAsync();
            if (afectados == 1)
            {
                if (ProductCategoryCache is null) return null;
                return ProductCategoryCache.TryRemove(id, out p);
            } 
            else
            {
                return null;
            }
        }

    }
}

