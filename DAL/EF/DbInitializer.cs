using DAL.Model;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DAL.EF
{
    public static class DbInitializer
    {
        public static void Initialize(AppDBContext dbContext, UserManager<User> userManager)
        {
            dbContext.Users.FirstOrDefault(i => i.Id == "fsd");
            if (userManager.Users.FirstOrDefault() == null)
            {
                User user = new User()
                {
                    FirstName = "Nik",
                    LastName = "Bahovez",
                    Email = "Bahovez123@gmail.com",
                    UserName = "NBA"
                };
                var resulr = userManager.CreateAsync(user, "aZ12345678*").GetAwaiter().GetResult();
                if (!resulr.Succeeded)
                    throw new Exception("Create user failed");
            }
        }
    }
}
