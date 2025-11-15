using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProyectoEcommerce.Models;
using System;

namespace ProyectoEcommerce.Data
{
    public class ProyectoEcommerceContext
       : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        public ProyectoEcommerceContext(DbContextOptions<ProyectoEcommerceContext> options)
    : base(options)
        {}


        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; }
        public DbSet<Buy> Buys { get; set; }
        public DbSet<Faq> Faqs { get; set; }
        public DbSet<BuyItem> BuyItems { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        public DbSet<Coupon> Coupons { get; set; }

        // Nuevo DbSet para promociones
        public DbSet<Promotion> Promotions { get; set; }

        public DbSet<Review> Reviews { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- CONFIGURACIÓN DE DECIMALES (NUEVO) ---
            modelBuilder.Entity<Buy>()
                .Property(b => b.IVA)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Buy>()
                .Property(b => b.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Buy>()
                .Property(b => b.Total)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BuyItem>()
                .Property(bi => bi.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BuyItem>()
                .Property(bi => bi.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            // --- ShoppingCartItem (join con payload) ---
            modelBuilder.Entity<ShoppingCartItem>()
                .HasKey(x => new { x.ShoppingCartId, x.ProductId });

            modelBuilder.Entity<ShoppingCartItem>()
                .HasOne(x => x.ShoppingCart)
                .WithMany(sc => sc.Items)
                .HasForeignKey(x => x.ShoppingCartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShoppingCartItem>()
                .HasOne(x => x.Product)
                .WithMany(p => p.ShoppingCartItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- BuyItem (join con payload) ---
            modelBuilder.Entity<BuyItem>()
                .HasKey(x => new { x.BuyId, x.ProductId });

            modelBuilder.Entity<BuyItem>()
                .HasOne(x => x.Buy)
                .WithMany(b => b.Items)
                .HasForeignKey(x => x.BuyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BuyItem>()
                .HasOne(x => x.Product)
                .WithMany(p => p.BuyItems)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice compuesto para Faq (ok)
            modelBuilder.Entity<Faq>()
                .HasIndex(f => new { f.Category, f.SortOrder });

            // --- FAVORITES: persistencia para usuarios y anónimos ---
            modelBuilder.Entity<Favorite>(entity =>
            {
                entity.HasKey(f => f.FavoriteId);

                // Propiedades
                entity.Property(f => f.UserId)
                      .HasMaxLength(450)   // coincide con AspNetUsers PK nvarchar(450)
                      .IsRequired(false);

                entity.Property(f => f.AnonymousId)
                      .HasMaxLength(450)
                      .IsRequired(false);

                // Mapear navegación User -> FK UserId (evita UserId1)
                entity.HasOne(f => f.User)
                      .WithMany()
                      .HasForeignKey(f => f.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Mapear relación con Product
                entity.HasOne(f => f.Product)
                      .WithMany()
                      .HasForeignKey(f => f.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(f => new { f.AnonymousId, f.ProductId }).HasDatabaseName("IX_Favorites_Anon_Product");
                entity.HasIndex(f => new { f.UserId, f.ProductId }).HasDatabaseName("IX_Favorites_User_Product");
            });

            // Promotions mapping
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.HasKey(p => p.PromotionId);
                entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
                entity.Property(p => p.BadgeText).HasMaxLength(50);
                entity.Property(p => p.DiscountPercent).HasPrecision(5, 2);
                entity.HasIndex(p => new { p.StartDate, p.EndDate });
            });

            // Product -> Promotion (optional)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Promotion)
                .WithMany(pr => pr.Products)
                .HasForeignKey(p => p.PromotionId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

