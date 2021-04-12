using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Payment.Dtos;
using Payment.Entities;

namespace Payment.DBContext
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
            
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customizations must go after base.OnModelCreating(builder)

            builder.Entity<AccountBalance>(config =>
            {
                // Set the default table name of AspNetUsers
                config.ToTable("Account_Balance");
            });
            builder.Entity<AccountBalance>().HasKey(r => new { r.AccountID, r.CurrencyType });

            builder.Entity<EntityOrder>(config =>
            {
                // Set the default table name of AspNetUsers
                config.ToView("EntityOrder");
                builder.Entity<EntityOrder>().HasKey(r => new { r.OrderID});
            });
        }
        public virtual DbSet<AccountBalance> Balances { get; set; }
        public virtual DbSet<EntityOrder> EntityOrders { get; set; }
    }
}
