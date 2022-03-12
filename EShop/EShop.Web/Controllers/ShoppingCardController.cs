using EShop.Web.Data;
using EShop.Web.Models.Domain;
using EShop.Web.Models.DTO;
using EShop.Web.Models.Relations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EShop.Web.Controllers
{
    public class ShoppingCardController : Controller
    {

        private readonly ApplicationDbContext _context;

        public ShoppingCardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var loggedInUser = await _context.Users.Where(z => z.Id == userId)
                .Include(z => z.UserCart)
                .Include("UserCart.ProductInShoppingCarts")
                .Include("UserCart.ProductInShoppingCarts.CurrnetProduct")
                .FirstOrDefaultAsync();

            var userCard = loggedInUser.UserCart;

            var allProducts = userCard.ProductInShoppingCarts.ToList();

            var allProductPrices = allProducts.Select(z => new
            {
                ProductPrice = z.CurrnetProduct.ProductPrice,
                Quantity = z.Quantity
            }).ToList();

            double totalPrice = 0.0;

            foreach (var item in allProductPrices)
            {
                totalPrice += item.Quantity * item.ProductPrice;
            }

            var reuslt = new ShoppingCartDto
            {
                Products = allProducts,
                TotalPrice = totalPrice
            };

            return View(reuslt);
        }

        public async Task<IActionResult> DeleteFromShoppingCart(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if(!string.IsNullOrEmpty(userId))
            {
                var loggedInUser = await _context.Users.Where(z => z.Id == userId)
                .Include(z => z.UserCart)
                .Include("UserCart.ProductInShoppingCarts")
                .Include("UserCart.ProductInShoppingCarts.CurrnetProduct")
                .FirstOrDefaultAsync();

                var userCard = loggedInUser.UserCart;

                var itemToDelete = userCard.ProductInShoppingCarts.Where(z => z.ProductId.Equals(id)).FirstOrDefault();

                userCard.ProductInShoppingCarts.Remove(itemToDelete);

                _context.Update(userCard);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "ShoppingCard");
        }
        
        public async Task<IActionResult> Order()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if(!string.IsNullOrEmpty(userId))
            {
                var loggedInUser = await _context.Users.Where(z => z.Id == userId)
                .Include(z => z.UserCart)
                .Include("UserCart.ProductInShoppingCarts")
                .Include("UserCart.ProductInShoppingCarts.CurrnetProduct")
                .FirstOrDefaultAsync();

                var userCard = loggedInUser.UserCart;

                Order order = new Order
                {
                    Id = Guid.NewGuid(),
                    User = loggedInUser,
                    UserId = userId
                };

                _context.Add(order);
                await _context.SaveChangesAsync();

                List<ProductInOrder> productInOrders = new List<ProductInOrder>();

                var result = userCard.ProductInShoppingCarts.Select(z => new ProductInOrder
                {
                    ProductId = z.CurrnetProduct.Id,
                    Product = z.CurrnetProduct,
                    OrderId = order.Id,
                    Order = order
                }).ToList();


                productInOrders.AddRange(result);

                foreach (var item in productInOrders)
                {
                    _context.Add(item);
                }

                await _context.SaveChangesAsync();


                loggedInUser.UserCart.ProductInShoppingCarts.Clear();

                _context.Update(loggedInUser);

                await _context.SaveChangesAsync();
                
            }

            return RedirectToAction("Index", "ShoppingCard");
        }
    }
}
