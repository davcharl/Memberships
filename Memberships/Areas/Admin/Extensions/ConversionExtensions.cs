using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Memberships.Areas.Admin.Models;
using Memberships.Entities;
using Memberships.Models;
using System.Transactions;

namespace Memberships.Areas.Admin.Extensions
{
    public static class ConversionExtensions
    {
        #region Product
        public static async Task<IEnumerable<ProductModel>> Convert(
            this IEnumerable<Product> products, ApplicationDbContext db)
        {
            if(products.Count().Equals(0))
                return new List<ProductModel>();
            
            var texts = await db.ProductLinkTexts.ToListAsync();
            var types = await db.ProductTypes.ToListAsync();

            return from p in products
                   select new ProductModel
                   {
                       Id = p.Id,
                       Title = p.Title,
                       Description = p.Description,
                       ImageUrl = p.ImageUrl,
                       ProductLinkTextId = p.ProductLinkTextId,
                       ProductTypeId = p.ProductTypeId,
                       ProductLinkTexts = texts,
                       ProductTypes = types
                   };
        }


        public static async Task<ProductModel> Convert(
            this Product product, ApplicationDbContext db)
        {
            var text = await db.ProductLinkTexts.FirstOrDefaultAsync(p => p.Id.Equals(product.ProductLinkTextId));
            var type = await db.ProductTypes.FirstOrDefaultAsync(p => p.Id.Equals(product.ProductTypeId));

            var model = new ProductModel
                   {
                       Id = product.Id,
                       Title = product.Title,
                       Description = product.Description,
                       ImageUrl = product.ImageUrl,
                       ProductLinkTextId = product.ProductLinkTextId,
                       ProductTypeId = product.ProductTypeId,
                       ProductLinkTexts  = new List<ProductLinkText>(), // text,
                       ProductTypes = new List<ProductType>()
                   };
            model.ProductLinkTexts.Add(text);
            model.ProductTypes.Add(type);

            return model;
        }
        #endregion

        #region ProductItem
        // Use IQueryable here because as there will be multiple async queries to the db here (Product and Item)
        public static async Task<IEnumerable<ProductItemModel>> Convert(
            this IQueryable<ProductItem> productitems, ApplicationDbContext db)
        {
            if (productitems.Count().Equals(0))
                return new List<ProductItemModel>();

            return await (from pi in productitems
                          select new ProductItemModel
                          {
                              ProductId = pi.ProductId,
                              ItemId = pi.ItemId,
                              ItemTitle = db.Items.FirstOrDefault( i=>i.Id.Equals(pi.ItemId)).Title,
                              ProductTitle = db.Products.FirstOrDefault( p=>p.Id.Equals(pi.ProductId)).Title
                          }).ToListAsync();
        }

        public static async Task<ProductItemModel> Convert(
            this ProductItem productitem, ApplicationDbContext db, bool addListData = true)
        {

            var model = new ProductItemModel
            {
                ItemId = productitem.ItemId,
                ProductId = productitem.ProductId,
                Items = addListData ? await db.Items.ToListAsync() : null,
                Products = addListData ? await db.Products.ToArrayAsync() : null,

                ItemTitle = (await db.Items.FirstOrDefaultAsync(i => 
                        i.Id.Equals(productitem.ItemId))).Title,
                ProductTitle = (await db.Products.FirstOrDefaultAsync(p => 
                        p.Id.Equals(productitem.ProductId))).Title
            };

            return model;
        }

        public static async Task<bool> CanChange(this ProductItem productItem, ApplicationDbContext db )
        {
            var oldPI = await db.ProductItems.CountAsync(pi =>
                    pi.ProductId.Equals(productItem.OldProductId) &&
                    pi.ItemId.Equals(productItem.OldItemId));

            var newPI = await db.ProductItems.CountAsync(pi =>
                    pi.ProductId.Equals(productItem.ProductId) &&
                    pi.ItemId.Equals(productItem.ItemId));

            return oldPI.Equals(1) && newPI.Equals(0);
        }

        public static async Task Change(this ProductItem productItem, ApplicationDbContext db)
        {
            var oldProductItem = await db.ProductItems.FirstOrDefaultAsync(pi =>
                    pi.ProductId.Equals(productItem.OldProductId) &&
                    pi.ItemId.Equals(productItem.OldItemId));

            var newProductItem = await db.ProductItems.FirstOrDefaultAsync(pi =>
                    pi.ProductId.Equals(productItem.ProductId) &&
                    pi.ItemId.Equals(productItem.ItemId));

            if(oldProductItem != null && newProductItem == null)
            {
                newProductItem = new ProductItem
                {
                    ProductId = productItem.ProductId,
                    ItemId = productItem.ItemId
                };

                using (var transaction = new TransactionScope(
                    TransactionScopeAsyncFlowOption.Enabled))
                {
                    try {
                        db.ProductItems.Remove(oldProductItem);
                        db.ProductItems.Add(newProductItem);

                        await db.SaveChangesAsync();
                        transaction.Complete();
                    }
                    catch { transaction.Dispose(); }
                }
            }
        }
        #endregion

        #region SubscriptionProduct
        // Use IQueryable here because as there will be multiple async queries to the db here (Product and Item)
        public static async Task<IEnumerable<SubscriptionProductModel>> Convert(
            this IQueryable<SubscriptionProduct> subscriptionproducts, ApplicationDbContext db)
        {
            if (subscriptionproducts.Count().Equals(0))
                return new List<SubscriptionProductModel>();

            return await (from sp in subscriptionproducts
                          select new SubscriptionProductModel
                          {
                              ProductId = sp.ProductId,
                              SubscriptionId = sp.SubscriptionId,
                              ProductTitle = db.Products.FirstOrDefault(p => p.Id.Equals(sp.ProductId)).Title,
                              SubscriptionTitle = db.Subscriptions.FirstOrDefault(s => s.Id.Equals(sp.SubscriptionId)).Title
                          }).ToListAsync();
        }

        public static async Task<SubscriptionProductModel> Convert(
            this SubscriptionProduct subscriptionproduct, ApplicationDbContext db, bool addListData = true)
        {

            var model = new SubscriptionProductModel
            {
                SubscriptionId = subscriptionproduct.SubscriptionId,
                ProductId = subscriptionproduct.ProductId,
                Subscriptions = addListData ? await db.Subscriptions.ToListAsync() : null,
                Products = addListData ? await db.Products.ToArrayAsync() : null,

                SubscriptionTitle = (await db.Subscriptions.FirstOrDefaultAsync(s =>
                        s.Id.Equals(subscriptionproduct.SubscriptionId))).Title,
                ProductTitle = (await db.Products.FirstOrDefaultAsync(p =>
                        p.Id.Equals(subscriptionproduct.ProductId))).Title
            };

            return model;
        }

        public static async Task<bool> CanChange(this SubscriptionProduct subscriptionproduct, ApplicationDbContext db)
        {
            var oldSP = await db.SubscriptionProducts.CountAsync(sp =>
                    sp.ProductId.Equals(subscriptionproduct.OldProductId) &&
                    sp.SubscriptionId.Equals(subscriptionproduct.OldSubscriptionId));

            var newSP = await db.SubscriptionProducts.CountAsync(sp =>
                    sp.ProductId.Equals(subscriptionproduct.ProductId) &&
                    sp.SubscriptionId.Equals(subscriptionproduct.SubscriptionId));

            return oldSP.Equals(1) && newSP.Equals(0);
        }

        public static async Task Change(this SubscriptionProduct subscriptionproduct, ApplicationDbContext db)
        {
            var oldSubscriptionProduct = await db.SubscriptionProducts.FirstOrDefaultAsync(osp =>
                    osp.ProductId.Equals(subscriptionproduct.OldProductId) &&
                    osp.SubscriptionId.Equals(subscriptionproduct.OldSubscriptionId));

            var newSubscriptionProduct = await db.SubscriptionProducts.FirstOrDefaultAsync(nsp =>
                    nsp.ProductId.Equals(subscriptionproduct.ProductId) &&
                    nsp.SubscriptionId.Equals(subscriptionproduct.SubscriptionId));

            if (oldSubscriptionProduct != null && newSubscriptionProduct == null)
            {
                newSubscriptionProduct = new SubscriptionProduct
                {
                    ProductId = subscriptionproduct.ProductId,
                    SubscriptionId = subscriptionproduct.SubscriptionId
                };

                using (var transaction = new TransactionScope(
                    TransactionScopeAsyncFlowOption.Enabled))
                {
                    try
                    {
                        db.SubscriptionProducts.Remove(oldSubscriptionProduct);
                        db.SubscriptionProducts.Add(newSubscriptionProduct);

                        await db.SaveChangesAsync();
                        transaction.Complete();
                    }
                    catch { transaction.Dispose(); }
                }
            }
        }
        #endregion

    }
}