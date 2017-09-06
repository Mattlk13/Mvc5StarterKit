﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Mvc5StarterKit.Models;
using Mvc5StarterKit.IzendaBoundary;

namespace Mvc5StarterKit
{
    public class EmailService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your email service here to send an email.
            return Task.FromResult(0);
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }

    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public async Task<ApplicationUser> FindTenantUserAsync(string tenant, string username, string password)
        {
            var passwordStore = Store as IUserPasswordStore<ApplicationUser>;

            var context = ApplicationDbContext.Create();

            var user = await context.Users
                .Include(x=> x.Tenant)
                .Where(x => x.UserName.Equals(username, StringComparison.InvariantCultureIgnoreCase))
                .Where(x=> x.Tenant.Name.Equals(tenant, StringComparison.InvariantCultureIgnoreCase))
                .SingleOrDefaultAsync();

            //var user = users.FirstOrDefault();

            if (await CheckPasswordAsync(user, password))
                return user;

            return null;
        }

        public async Task<ApplicationUser> FindADUserAsync(string username, string password)
        {
            var context = ApplicationDbContext.Create();
            var user = await context.Users
                .Where(x => x.UserName.Equals(username, StringComparison.InvariantCultureIgnoreCase))
                .SingleOrDefaultAsync();
            if (LDAPService.GetInstance().Authenticate(username, password))
                return user;
            return null;
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context) 
        {
            var manager = new ApplicationUserManager(new UserStore<ApplicationUser>(context.Get<ApplicationDbContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
            };

            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug it in here.
            manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<ApplicationUser>
            {
                MessageFormat = "Your security code is {0}"
            });
            manager.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<ApplicationUser>
            {
                Subject = "Security Code",
                BodyFormat = "Your security code is {0}"
            });
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = 
                    new DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }

    // Configure the application sign-in manager which is used in this application.
    public class ApplicationSignInManager : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public async Task<bool> ADSigninAsync(string username, string password, bool remember)
        {
            var user = await (this.UserManager as ApplicationUserManager).FindADUserAsync(username, password);
            if (user != null)
            {
                var identity = new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie);
                identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));

                var role = (await UserManager.GetRolesAsync(user.Id)).FirstOrDefault();
                if(role != null)
                {
                    identity.AddClaim(new Claim(ClaimsIdentity.DefaultRoleClaimType, role));
                }
                
                AuthenticationManager.SignIn(identity);

                return true;
            }
            return false;
        }

        public async Task<bool> PasswordSigninAsync(string tenant, string username, string password, bool remember)
        {
            var user = await (this.UserManager as ApplicationUserManager).FindTenantUserAsync(tenant, username, password);

            if(user != null)
            {
                await SignInAsync(user, remember, true);
                return true;
            }
            
            return false;
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }
}
